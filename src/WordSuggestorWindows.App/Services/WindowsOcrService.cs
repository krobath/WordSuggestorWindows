using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsOcrService
{
    private static readonly TimeSpan LegacyScreenClipClipboardTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan SnippingToolCallbackTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan CallbackPollInterval = TimeSpan.FromMilliseconds(250);
    private const string OcrUserAgent = "WordSuggestor";
    private static readonly string OcrFlowDiagnosticLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WordSuggestor",
        "diagnostics",
        "ocr-flow.log");
    private enum LegacyScreenClipAttemptStatus
    {
        Imported,
        LaunchUnavailable,
        NoImportResult
    }
    private const string OcrBridgeScript = """
        param(
            [Parameter(Mandatory = $true)]
            [string] $ImagePath
        )

        $ErrorActionPreference = 'Stop'
        [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

        Add-Type -AssemblyName System.Runtime.WindowsRuntime
        $null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
        $null = [Windows.Storage.FileAccessMode, Windows.Storage, ContentType = WindowsRuntime]
        $null = [Windows.Storage.Streams.IRandomAccessStream, Windows.Storage.Streams, ContentType = WindowsRuntime]
        $null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics.Imaging, ContentType = WindowsRuntime]
        $null = [Windows.Graphics.Imaging.SoftwareBitmap, Windows.Graphics.Imaging, ContentType = WindowsRuntime]
        $null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType = WindowsRuntime]
        $null = [Windows.Media.Ocr.OcrResult, Windows.Foundation, ContentType = WindowsRuntime]

        function Await($Operation, [Type] $ResultType) {
            $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() |
                Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 } |
                Select-Object -First 1
            $task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
            $task.GetAwaiter().GetResult()
        }

        $file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($ImagePath)) ([Windows.Storage.StorageFile])
        $stream = Await ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])

        try {
            $decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
            $bitmap = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])

            try {
                $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
                if ($null -eq $engine) {
                    exit 20
                }

                $result = Await ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
                $lines = @($result.Lines | ForEach-Object { $_.Text } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
                [Console]::Out.Write(($lines -join [Environment]::NewLine))
            }
            finally {
                if ($bitmap -is [System.IDisposable]) {
                    $bitmap.Dispose()
                }
            }
        }
        finally {
            if ($stream -is [System.IDisposable]) {
                $stream.Dispose()
            }
        }
        """;
    private const string SharedStorageTokenBridgeScript = """
        param(
            [Parameter(Mandatory = $true)]
            [string] $Token,

            [Parameter(Mandatory = $true)]
            [string] $OutputPath
        )

        $ErrorActionPreference = 'Stop'
        [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

        Add-Type -AssemblyName System.Runtime.WindowsRuntime
        $null = [Windows.ApplicationModel.DataTransfer.SharedStorageAccessManager, Windows.ApplicationModel.DataTransfer, ContentType = WindowsRuntime]
        $null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
        $null = [Windows.Storage.StorageFolder, Windows.Storage, ContentType = WindowsRuntime]
        $null = [Windows.Storage.NameCollisionOption, Windows.Storage, ContentType = WindowsRuntime]

        function Await($Operation, [Type] $ResultType) {
            $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() |
                Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 } |
                Select-Object -First 1
            $task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
            $task.GetAwaiter().GetResult()
        }

        $outputDirectory = Split-Path -Parent $OutputPath
        $outputName = Split-Path -Leaf $OutputPath
        $folder = Await ([Windows.Storage.StorageFolder]::GetFolderFromPathAsync($outputDirectory)) ([Windows.Storage.StorageFolder])
        $file = Await ([Windows.ApplicationModel.DataTransfer.SharedStorageAccessManager]::RedeemTokenForFileAsync($Token)) ([Windows.Storage.StorageFile])
        $copy = Await ($file.CopyAsync($folder, $outputName, [Windows.Storage.NameCollisionOption]::ReplaceExisting)) ([Windows.Storage.StorageFile])
        [Console]::Out.Write($copy.Path)
        """;

    public async Task<OcrImportResult?> CaptureScreenAndRecognizeAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("D");
        EmitDiagnostic(correlationId, "Capture started.");
        WindowsOcrCallbackBridge.DeleteCallback(correlationId);

        try
        {
            var (legacyClipboardResult, legacyStatus) = await TryCaptureWithLegacyClipboardScreenClipAsync(correlationId, cancellationToken);
            if (legacyClipboardResult is not null)
            {
                return legacyClipboardResult;
            }

            if (legacyStatus is not LegacyScreenClipAttemptStatus.LaunchUnavailable)
            {
                EmitDiagnostic(correlationId, $"Legacy screenclip path ended without import result. status={legacyStatus}");
                return null;
            }

            EmitDiagnostic(correlationId, "Legacy screenclip path produced no import result. Falling back to redirect callback flow.");

            if (!TryEnsureCallbackProtocolRegistration(correlationId))
            {
                EmitDiagnostic(correlationId, "Capture stopped: callback protocol registration failed.");
                return null;
            }

            if (!TryLaunchSnippingToolProtocol(correlationId))
            {
                EmitDiagnostic(correlationId, "Capture stopped: Snipping Tool protocol launch failed.");
                return null;
            }

            var callback = await WaitForCallbackAsync(correlationId, cancellationToken);
            if (callback is null || !callback.IsSuccess)
            {
                EmitDiagnostic(
                    correlationId,
                    callback is null
                        ? "Capture stopped: no callback before timeout."
                        : $"Capture stopped: callback code={callback.Code}, reason={callback.Reason ?? "n/a"}, tokenPresent={callback.Token is not null}.");
                return null;
            }

            EmitDiagnostic(correlationId, $"Callback received: code={callback.Code}, tokenLength={callback.Token?.Length ?? 0}.");
            var tempPath = ResolveTempPngPath("wordsuggestor-ocr-snip");
            try
            {
                var imagePath = tempPath;
                if (string.IsNullOrWhiteSpace(callback.Token))
                {
                    EmitDiagnostic(correlationId, "OCR stopped: callback succeeded without a redeemable file access token.");
                    return null;
                }

                EmitDiagnostic(correlationId, $"Redeeming shared-storage token to temp image: {tempPath}");
                var redeemedPath = await RedeemSharedStorageTokenAsync(callback.Token, tempPath, correlationId, cancellationToken);
                if (string.IsNullOrWhiteSpace(redeemedPath) || !File.Exists(redeemedPath))
                {
                    EmitDiagnostic(correlationId, $"Token redemption failed or file missing. redeemedPath={redeemedPath ?? "null"}");
                    return null;
                }

                imagePath = redeemedPath;
                var fileInfo = new FileInfo(redeemedPath);
                EmitDiagnostic(correlationId, $"Token redeemed: path={redeemedPath}, bytes={fileInfo.Length}");
                var text = await RecognizeTextFromImageAsync(imagePath, correlationId, cancellationToken);
                if (string.IsNullOrWhiteSpace(text))
                {
                    EmitDiagnostic(correlationId, "OCR stopped: recognized text was empty.");
                    return null;
                }

                var normalized = NormalizeOcrText(text);
                EmitDiagnostic(correlationId, $"OCR normalized text: chars={normalized.Length}");
                if (!TrySetClipboardText(normalized))
                {
                    EmitDiagnostic(correlationId, "OCR stopped: could not copy recognized text to clipboard.");
                    return null;
                }

                EmitDiagnostic(correlationId, $"OCR completed: chars={normalized.Length}, lines={normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length}");
                return new OcrImportResult(
                    normalized,
                    "Windows screen snip OCR",
                    normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                    DateTimeOffset.Now);
            }
            finally
            {
                EmitDiagnostic(correlationId, $"Deleting temp image if present: {tempPath}");
                TryDeleteTempFile(tempPath);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            EmitDiagnostic(correlationId, $"Capture failed with exception: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
        finally
        {
            EmitDiagnostic(correlationId, "Deleting callback file if present.");
            WindowsOcrCallbackBridge.DeleteCallback(correlationId);
        }
    }

    private async Task<(OcrImportResult? Result, LegacyScreenClipAttemptStatus Status)> TryCaptureWithLegacyClipboardScreenClipAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        IDataObject? originalClipboard = null;
        var sentinel = $"__WordSuggestorOcrClipboardProbe_{Guid.NewGuid():N}__";

        try
        {
            originalClipboard = TryGetClipboardDataObject();
            if (!TrySetClipboardSentinel(sentinel))
            {
                EmitDiagnostic(correlationId, "Legacy screenclip path unavailable: could not place clipboard sentinel.");
                return (null, LegacyScreenClipAttemptStatus.LaunchUnavailable);
            }

            if (!TryLaunchLegacyScreenClipProtocol(correlationId))
            {
                EmitDiagnostic(correlationId, "Legacy screenclip launch failed.");
                var restoredAfterLaunchFailure = RestoreClipboard(originalClipboard);
                EmitDiagnostic(correlationId, restoredAfterLaunchFailure
                    ? "Legacy screenclip launch failure restored clipboard snapshot."
                    : "Legacy screenclip launch failure could not restore clipboard snapshot.");
                return (null, LegacyScreenClipAttemptStatus.LaunchUnavailable);
            }

            var legacyImagePath = await WaitForClipboardImageAsync(correlationId, sentinel, cancellationToken);
            if (string.IsNullOrWhiteSpace(legacyImagePath))
            {
                EmitDiagnostic(correlationId, "Legacy screenclip stopped: no clipboard image arrived before timeout or cancellation.");
                var restoredAfterTimeout = RestoreClipboard(originalClipboard);
                EmitDiagnostic(correlationId, restoredAfterTimeout
                    ? "Legacy screenclip timeout restored clipboard snapshot."
                    : "Legacy screenclip timeout could not restore clipboard snapshot.");
                return (null, LegacyScreenClipAttemptStatus.NoImportResult);
            }

            try
            {
                var fileInfo = new FileInfo(legacyImagePath);
                EmitDiagnostic(correlationId, $"Legacy screenclip clipboard image saved: path={legacyImagePath}, bytes={fileInfo.Length}");
                var text = await RecognizeTextFromImageAsync(legacyImagePath, correlationId, cancellationToken);
                if (string.IsNullOrWhiteSpace(text))
                {
                    EmitDiagnostic(correlationId, "Legacy screenclip stopped: recognized text was empty.");
                    return (null, LegacyScreenClipAttemptStatus.NoImportResult);
                }

                var normalized = NormalizeOcrText(text);
                EmitDiagnostic(correlationId, $"Legacy screenclip OCR normalized text: chars={normalized.Length}");
                if (!TrySetClipboardText(normalized))
                {
                    EmitDiagnostic(correlationId, "Legacy screenclip stopped: could not copy recognized text to clipboard.");
                    return (null, LegacyScreenClipAttemptStatus.NoImportResult);
                }

                EmitDiagnostic(correlationId, $"Legacy screenclip OCR completed: chars={normalized.Length}, lines={normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length}");
                return (
                    new OcrImportResult(
                        normalized,
                        "Windows screen snip OCR",
                        normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                        DateTimeOffset.Now),
                    LegacyScreenClipAttemptStatus.Imported);
            }
            finally
            {
                EmitDiagnostic(correlationId, $"Deleting legacy clipboard temp image if present: {legacyImagePath}");
                TryDeleteTempFile(legacyImagePath);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            EmitDiagnostic(correlationId, $"Legacy screenclip failed with exception: {ex.GetType().Name}: {ex.Message}");
            var restoredAfterException = RestoreClipboard(originalClipboard);
            EmitDiagnostic(correlationId, restoredAfterException
                ? "Legacy screenclip exception restored clipboard snapshot."
                : "Legacy screenclip exception could not restore clipboard snapshot.");
            return (null, LegacyScreenClipAttemptStatus.NoImportResult);
        }
    }

    private static bool TryLaunchLegacyScreenClipProtocol(string correlationId)
    {
        const string uri = "ms-screenclip:?source=WordSuggestor&clippingMode=Rectangle";

        try
        {
            EmitDiagnostic(correlationId, $"Launching legacy screenclip URI: {uri}");
            var startedProcess = Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            EmitDiagnostic(correlationId, $"Legacy screenclip protocol launched. processStarted={startedProcess is not null}");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static bool TryLaunchSnippingToolProtocol(string correlationId)
    {
        var uri = BuildSnippingToolCaptureUri(correlationId);

        try
        {
            EmitDiagnostic(correlationId, $"Launching Snipping Tool URI: {uri}");
            var startedProcess = Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            EmitDiagnostic(correlationId, $"Snipping Tool protocol launched. processStarted={startedProcess is not null}");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static string BuildSnippingToolCaptureUri(string correlationId)
    {
        // Keep the launch contract as close as possible to the current Microsoft examples:
        // mode + user-agent + redirect-uri (+ optional correlation id).
        var query = string.Join(
            "&",
            "rectangle",
            $"user-agent={Uri.EscapeDataString(OcrUserAgent)}",
            $"x-request-correlation-id={Uri.EscapeDataString(correlationId)}",
            $"redirect-uri={Uri.EscapeDataString(WindowsOcrCallbackBridge.CallbackUri)}");
        return $"ms-screenclip://capture/image?{query}";
    }

    private static bool TryEnsureCallbackProtocolRegistration(string correlationId)
    {
        var executablePath = Environment.ProcessPath ??
            Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            EmitDiagnostic(correlationId, "Callback protocol registration failed: executable path unavailable.");
            return false;
        }

        try
        {
            using var schemeKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{WindowsOcrCallbackBridge.CallbackScheme}");
            if (schemeKey is null)
            {
                return false;
            }

            schemeKey.SetValue(string.Empty, "URL:WordSuggestor OCR Callback");
            schemeKey.SetValue("URL Protocol", string.Empty);

            using var commandKey = schemeKey.CreateSubKey(@"shell\open\command");
            commandKey?.SetValue(string.Empty, $"\"{executablePath}\" \"%1\"");
            EmitDiagnostic(correlationId, $"Callback protocol registered under HKCU for executable: {executablePath}");
            return commandKey is not null;
        }
        catch (UnauthorizedAccessException)
        {
            EmitDiagnostic(correlationId, "Callback protocol registration failed: unauthorized access.");
            return false;
        }
        catch (IOException)
        {
            EmitDiagnostic(correlationId, "Callback protocol registration failed: IO error.");
            return false;
        }
    }

    private static async Task<OcrScreenClipCallback?> WaitForCallbackAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        EmitDiagnostic(
            correlationId,
            $"Waiting for callback file(s): {string.Join(" | ", WindowsOcrCallbackBridge.ResolveCallbackPaths(correlationId))}");
        var deadline = DateTimeOffset.Now + SnippingToolCallbackTimeout;
        while (DateTimeOffset.Now < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var callback = WindowsOcrCallbackBridge.TryReadCallback(correlationId);
            if (callback is not null)
            {
                return callback;
            }

            await Task.Delay(CallbackPollInterval, cancellationToken);
        }

        return null;
    }

    private static async Task<string?> WaitForClipboardImageAsync(
        string correlationId,
        string sentinel,
        CancellationToken cancellationToken)
    {
        EmitDiagnostic(correlationId, "Waiting for clipboard image from legacy screenclip path.");
        var deadline = DateTimeOffset.Now + LegacyScreenClipClipboardTimeout;
        while (DateTimeOffset.Now < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryPersistClipboardImageToTempFile(correlationId, sentinel, out var imagePath))
            {
                return imagePath;
            }

            await Task.Delay(CallbackPollInterval, cancellationToken);
        }

        return null;
    }

    private static async Task<string?> RecognizeTextFromImageAsync(
        string imagePath,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"wordsuggestor-ocr-bridge-{Guid.NewGuid():N}.ps1");
        await File.WriteAllTextAsync(scriptPath, OcrBridgeScript, Encoding.UTF8, cancellationToken);

        try
        {
            EmitDiagnostic(correlationId, $"Starting OCR bridge: imagePath={imagePath}, exists={File.Exists(imagePath)}");
            var startInfo = CreatePowerShellStartInfo(scriptPath);
            startInfo.ArgumentList.Add("-ImagePath");
            startInfo.ArgumentList.Add(imagePath);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                EmitDiagnostic(correlationId, "OCR bridge failed to start.");
                return null;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;
            EmitDiagnostic(correlationId, $"OCR bridge exited: code={process.ExitCode}, stdoutChars={output.Length}, stderr={SanitizeForLog(error, null)}");
            return process.ExitCode == 0
                ? output.Trim()
                : null;
        }
        finally
        {
            TryDeleteTempFile(scriptPath);
        }
    }

    private static async Task<string?> RedeemSharedStorageTokenAsync(
        string token,
        string outputPath,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"wordsuggestor-ocr-token-bridge-{Guid.NewGuid():N}.ps1");
        await File.WriteAllTextAsync(scriptPath, SharedStorageTokenBridgeScript, Encoding.UTF8, cancellationToken);

        try
        {
            EmitDiagnostic(correlationId, $"Starting token bridge: outputPath={outputPath}");
            var startInfo = CreatePowerShellStartInfo(scriptPath);
            startInfo.ArgumentList.Add("-Token");
            startInfo.ArgumentList.Add(token);
            startInfo.ArgumentList.Add("-OutputPath");
            startInfo.ArgumentList.Add(outputPath);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                EmitDiagnostic(correlationId, "Token bridge failed to start.");
                return null;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;
            EmitDiagnostic(correlationId, $"Token bridge exited: code={process.ExitCode}, stdoutChars={output.Length}, stderr={SanitizeForLog(error, token)}");
            return process.ExitCode == 0
                ? output.Trim()
                : null;
        }
        finally
        {
            TryDeleteTempFile(scriptPath);
        }
    }

    private static string ResolveTempPngPath(string prefix) =>
        Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}.png");

    private static bool TryPersistClipboardImageToTempFile(
        string correlationId,
        string sentinel,
        out string? imagePath)
    {
        imagePath = null;

        try
        {
            if (!Clipboard.ContainsImage())
            {
                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                {
                    var clipboardText = Clipboard.GetText(TextDataFormat.UnicodeText);
                    if (string.Equals(clipboardText, sentinel, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return false;
            }

            var bitmap = Clipboard.GetImage();
            if (bitmap is null)
            {
                EmitDiagnostic(correlationId, "Legacy screenclip clipboard indicated an image, but GetImage returned null.");
                return false;
            }

            var frozen = bitmap.Clone();
            frozen.Freeze();
            imagePath = ResolveTempPngPath("wordsuggestor-ocr-legacy-screenclip");
            using var stream = File.Create(imagePath);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(frozen));
            encoder.Save(stream);
            EmitDiagnostic(correlationId, $"Legacy screenclip clipboard image captured to temp file: {imagePath}");
            return true;
        }
        catch (COMException ex)
        {
            EmitDiagnostic(correlationId, $"Legacy screenclip clipboard probe COMException: {ex.Message}");
            return false;
        }
        catch (ExternalException ex)
        {
            EmitDiagnostic(correlationId, $"Legacy screenclip clipboard probe ExternalException: {ex.Message}");
            return false;
        }
        catch (IOException ex)
        {
            EmitDiagnostic(correlationId, $"Legacy screenclip clipboard image save failed: {ex.Message}");
            return false;
        }
    }

    private static ProcessStartInfo CreatePowerShellStartInfo(string scriptPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        return startInfo;
    }

    private static string NormalizeOcrText(string text)
    {
        var normalized = text
            .Normalize()
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();

        if (normalized.Length == 0)
        {
            return normalized;
        }

        var lines = normalized.Split('\n');
        var output = new StringBuilder(normalized.Length);
        var suppressNextJoinSpace = false;
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd();
            if (line.Length == 0)
            {
                AppendHardBreak(output);
                continue;
            }

            if (output.Length > 0 &&
                output[^1] != '\n' &&
                !char.IsWhiteSpace(output[^1]) &&
                !suppressNextJoinSpace)
            {
                output.Append(' ');
            }

            suppressNextJoinSpace = false;
            output.Append(line.TrimStart());

            if (index >= lines.Length - 1)
            {
                continue;
            }

            var nextLine = lines[index + 1];
            if (ShouldPreserveOcrLineBreak(nextLine))
            {
                AppendHardBreak(output);
                continue;
            }

            if (output.Length > 0 && IsHyphenationMarker(output[^1]))
            {
                output.Length--;
                suppressNextJoinSpace = true;
            }
        }

        return output.ToString().Trim();
    }

    private static bool ShouldPreserveOcrLineBreak(string nextLine)
    {
        if (string.IsNullOrWhiteSpace(nextLine))
        {
            return true;
        }

        var leadingWhitespace = nextLine.Length - nextLine.TrimStart().Length;
        if (leadingWhitespace >= 2 || nextLine.StartsWith('\t'))
        {
            return true;
        }

        var trimmed = nextLine.TrimStart();
        if (trimmed.Length == 0)
        {
            return true;
        }

        return IsBulletMarker(trimmed[0]) ||
            (char.IsDigit(trimmed[0]) && trimmed.Length > 1 && trimmed[1] is '.' or ')');
    }

    private static bool IsBulletMarker(char c) =>
        c is '\u2022' or '-' or '*' or '\u2023' or '\u00B7';

    private static bool IsHyphenationMarker(char c) =>
        c is '-' or '\u2010' or '\u2011';

    private static void AppendHardBreak(StringBuilder builder)
    {
        if (builder.Length == 0 || builder[^1] != '\n')
        {
            builder.Append('\n');
        }
    }

    private static bool TrySetClipboardText(string text)
    {
        try
        {
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    private static IDataObject? TryGetClipboardDataObject()
    {
        try
        {
            return Clipboard.GetDataObject();
        }
        catch (COMException)
        {
            return null;
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    private static bool TrySetClipboardSentinel(string sentinel)
    {
        try
        {
            Clipboard.SetText(sentinel, TextDataFormat.UnicodeText);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    private static bool RestoreClipboard(IDataObject? originalClipboard)
    {
        try
        {
            if (originalClipboard is null)
            {
                Clipboard.Clear();
                return true;
            }

            Clipboard.SetDataObject(originalClipboard, copy: true);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void EmitDiagnostic(string correlationId, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OcrFlowDiagnosticLogPath)!);
            File.AppendAllText(
                OcrFlowDiagnosticLogPath,
                $"{DateTimeOffset.Now:O} [{correlationId}] {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string SanitizeForLog(string value, string? secret)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        var sanitized = string.IsNullOrWhiteSpace(secret)
            ? value
            : value.Replace(secret, "<redacted-token>", StringComparison.Ordinal);
        sanitized = sanitized.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
        return sanitized.Length <= 600 ? sanitized : sanitized[..600] + "...";
    }

}

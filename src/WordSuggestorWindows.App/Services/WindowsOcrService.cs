using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsOcrService
{
    private static readonly TimeSpan ClipboardImageTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan ClipboardPollInterval = TimeSpan.FromMilliseconds(250);
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const ushort VirtualKeyLeftWindows = 0x5B;
    private const ushort VirtualKeyShift = 0x10;
    private const ushort VirtualKeyS = 0x53;
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

    public async Task<OcrImportResult?> CaptureScreenAndRecognizeAsync(CancellationToken cancellationToken = default)
    {
        var originalClipboard = TryGetClipboardDataObject();
        var shouldRestoreClipboard = true;
        var sentinel = $"__WordSuggestorOcrProbe_{Guid.NewGuid():N}__";

        try
        {
            if (!TrySetClipboardText(sentinel))
            {
                return null;
            }

            if (!TryStartScreenSnipOverlay())
            {
                return null;
            }

            var image = await WaitForClipboardImageAsync(cancellationToken);
            if (image is null)
            {
                return null;
            }

            var tempPath = SaveBitmapSourceToTempPng(image);
            try
            {
                var text = await RecognizeTextFromImageAsync(tempPath, cancellationToken);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                var normalized = NormalizeOcrText(text);
                if (!TrySetClipboardText(normalized))
                {
                    return null;
                }

                shouldRestoreClipboard = false;
                return new OcrImportResult(
                    normalized,
                    "Windows screen snip OCR",
                    normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                    DateTimeOffset.Now);
            }
            finally
            {
                TryDeleteTempFile(tempPath);
            }
        }
        finally
        {
            if (shouldRestoreClipboard)
            {
                RestoreClipboard(originalClipboard);
            }
        }
    }

    private static bool TryStartScreenSnipOverlay()
    {
        var inputs = new[]
        {
            KeyboardInput(VirtualKeyLeftWindows, keyUp: false),
            KeyboardInput(VirtualKeyShift, keyUp: false),
            KeyboardInput(VirtualKeyS, keyUp: false),
            KeyboardInput(VirtualKeyS, keyUp: true),
            KeyboardInput(VirtualKeyShift, keyUp: true),
            KeyboardInput(VirtualKeyLeftWindows, keyUp: true)
        };

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) == inputs.Length;
    }

    private static async Task<BitmapSource?> WaitForClipboardImageAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.Now + ClipboardImageTimeout;
        while (DateTimeOffset.Now < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var image = TryGetClipboardImage();
            if (image is not null)
            {
                return image;
            }

            await Task.Delay(ClipboardPollInterval, cancellationToken);
        }

        return null;
    }

    private static BitmapSource? TryGetClipboardImage()
    {
        try
        {
            return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
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

    private static async Task<string?> RecognizeTextFromImageAsync(string imagePath, CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"wordsuggestor-ocr-bridge-{Guid.NewGuid():N}.ps1");
        await File.WriteAllTextAsync(scriptPath, OcrBridgeScript, Encoding.UTF8, cancellationToken);

        try
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
            startInfo.ArgumentList.Add("-ImagePath");
            startInfo.ArgumentList.Add(imagePath);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            _ = await errorTask;
            return process.ExitCode == 0
                ? output.Trim()
                : null;
        }
        finally
        {
            TryDeleteTempFile(scriptPath);
        }
    }

    private static string SaveBitmapSourceToTempPng(BitmapSource image)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"wordsuggestor-ocr-{Guid.NewGuid():N}.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = File.Create(tempPath);
        encoder.Save(stream);
        return tempPath;
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

    private static void RestoreClipboard(IDataObject? originalClipboard)
    {
        try
        {
            if (originalClipboard is null)
            {
                Clipboard.Clear();
                return;
            }

            Clipboard.SetDataObject(originalClipboard, copy: true);
        }
        catch (COMException)
        {
        }
        catch (ExternalException)
        {
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

    private static INPUT KeyboardInput(ushort virtualKey, bool keyUp) =>
        new()
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    dwFlags = keyUp ? KeyEventKeyUp : 0
                }
            }
        };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }
}

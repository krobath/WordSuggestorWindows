using System.Diagnostics;
using System.IO;
using System.Text;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsTextToSpeechService : IDisposable
{
    private static readonly string DiagnosticLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WordSuggestor",
        "diagnostics",
        "tts-flow.log");
    private static readonly Lazy<OneCoreRuntimeStatus> OneCoreRuntimeStatusCache =
        new(ProbeOneCoreRuntime, LazyThreadSafetyMode.ExecutionAndPublication);

    private Process? _process;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? SpeechStopped;

    public bool IsSpeaking => _process is { HasExited: false };

    public static OneCoreRuntimeStatus GetOneCoreRuntimeStatus() => OneCoreRuntimeStatusCache.Value;

    public TextToSpeechInvocationResult Speak(string text, TtsSpeechOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            WriteDiagnostic("Speak skipped: empty text.");
            return new TextToSpeechInvocationResult(
                WindowsVoiceCatalogService.SapiDesktopSource,
                "Windows standardstemme",
                WindowsVoiceCatalogService.SapiDesktopSource,
                "Ingen tekst fundet til oplæsning.");
        }

        Stop();
        WriteDiagnostic(
            $"Speak requested: chars={text.Trim().Length}, lang={options.LanguageCode}, voice={options.VoiceDisplayName ?? "default"}, source={options.VoiceSource ?? "unknown"}, voiceIdPresent={!string.IsNullOrWhiteSpace(options.VoiceId)}, useSystemSettings={options.UseSystemSpeechSettings}, speedDelta={options.ReadingSpeedDelta}, fallback={options.FallbackReason ?? "none"}.");

        var invocation = ResolveInvocation(text, options);
        WriteDiagnostic(
            $"Speak dispatch: backend={invocation.Backend}, voice={invocation.VoiceDisplayName}, source={invocation.VoiceSource}, fallback={invocation.FallbackReason ?? "none"}.");

        _process = Process.Start(invocation.StartInfo)
            ?? throw new InvalidOperationException("Windows TTS bridge kunne ikke startes.");
        _process.EnableRaisingEvents = true;
        _process.Exited += ProcessOnExited;
        StatusChanged?.Invoke(
            this,
            invocation.FallbackReason is null
                ? $"Oplæsning startet med {invocation.VoiceDisplayName} ({text.Length} tegn)."
                : $"Oplæsning bruger fallback: {invocation.FallbackReason}");
        return new TextToSpeechInvocationResult(
            invocation.Backend,
            invocation.VoiceDisplayName,
            invocation.VoiceSource,
            invocation.FallbackReason);
    }

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill();
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            CleanupProcess();
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private InvocationPlan ResolveInvocation(string text, TtsSpeechOptions options)
    {
        if (string.Equals(options.VoiceSource, WindowsVoiceCatalogService.OneCoreSource, StringComparison.OrdinalIgnoreCase))
        {
            var oneCoreStatus = GetOneCoreRuntimeStatus();
            if (oneCoreStatus.IsAvailable)
            {
                return new InvocationPlan(
                    CreateOneCoreProcessStartInfo(text, options),
                    WindowsVoiceCatalogService.OneCoreSource,
                    options.VoiceDisplayName ?? "Windows OneCore-stemme",
                    WindowsVoiceCatalogService.OneCoreSource,
                    options.FallbackReason);
            }

            var sapiFallback = WindowsVoiceCatalogService.ResolveVoiceBySource(
                options.LanguageCode,
                preferredVoiceId: null,
                WindowsVoiceCatalogService.SapiDesktopSource);
            var fallbackOptions = options with
            {
                VoiceId = sapiFallback.Voice?.Id,
                VoiceDisplayName = sapiFallback.Voice?.DisplayName ?? "Windows standardstemme",
                VoiceSource = sapiFallback.Voice?.Source ?? WindowsVoiceCatalogService.SapiDesktopSource,
                FallbackReason = CombineFallbackReasons(
                    options.FallbackReason,
                    $"OneCore-backend utilgængelig: {oneCoreStatus.Detail}",
                    sapiFallback.FallbackReason)
            };
            return new InvocationPlan(
                CreateSapiProcessStartInfo(text, fallbackOptions),
                WindowsVoiceCatalogService.SapiDesktopSource,
                fallbackOptions.VoiceDisplayName ?? "Windows standardstemme",
                fallbackOptions.VoiceSource ?? WindowsVoiceCatalogService.SapiDesktopSource,
                fallbackOptions.FallbackReason);
        }

        return new InvocationPlan(
            CreateSapiProcessStartInfo(text, options),
            WindowsVoiceCatalogService.SapiDesktopSource,
            options.VoiceDisplayName ?? "Windows standardstemme",
            options.VoiceSource ?? WindowsVoiceCatalogService.SapiDesktopSource,
            options.FallbackReason);
    }

    private void ProcessOnExited(object? sender, EventArgs e)
    {
        var process = _process;
        var status = "Oplæsning stoppet.";
        if (process is not null)
        {
            try
            {
                var stderr = process.StandardError.ReadToEnd();
                var stdout = process.StandardOutput.ReadToEnd();
                var exitCode = process.ExitCode;
                WriteDiagnostic(
                    $"Speak process exited: exitCode={exitCode}, stdoutChars={stdout.Length}, stderr={TrimForDiagnostic(stderr)}.");
                status = exitCode == 0
                    ? "Oplæsning færdig."
                    : $"Oplæsning fejlede med exit code {exitCode}. Se tts-flow.log.";
            }
            catch (InvalidOperationException ex)
            {
                WriteDiagnostic($"Speak process exit inspection failed: {ex.Message}");
            }
        }

        CleanupProcess();
        SpeechStopped?.Invoke(this, status);
    }

    private void CleanupProcess()
    {
        if (_process is null)
        {
            return;
        }

        _process.Exited -= ProcessOnExited;
        _process.Dispose();
        _process = null;
    }

    private static OneCoreRuntimeStatus ProbeOneCoreRuntime()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Sta -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {EncodePowerShellCommand(BuildOneCoreProbeCommand())}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new OneCoreRuntimeStatus(false, "OneCore probe-processen kunne ikke startes.");
            }

            process.WaitForExit();
            var stdout = process.StandardOutput.ReadToEnd().Trim();
            var stderr = TrimForDiagnostic(process.StandardError.ReadToEnd());
            if (process.ExitCode == 0)
            {
                return new OneCoreRuntimeStatus(true, string.IsNullOrWhiteSpace(stdout) ? "OneCore probe ok." : stdout);
            }

            return new OneCoreRuntimeStatus(
                false,
                string.IsNullOrWhiteSpace(stderr)
                    ? $"OneCore probe fejlede med exit code {process.ExitCode}."
                    : stderr);
        }
        catch (Exception ex)
        {
            return new OneCoreRuntimeStatus(false, ex.Message);
        }
    }

    private static ProcessStartInfo CreateSapiProcessStartInfo(string text, TtsSpeechOptions options)
    {
        var command = "$ErrorActionPreference = 'Stop'; " +
            "$speaker = New-Object -ComObject SAPI.SpVoice; " +
            BuildSapiVoiceSelectionCommand(options) +
            BuildSapiRateCommand(options) +
            $"$null = $speaker.Speak('{EscapePowerShellSingleQuotedString(text)}')";

        return new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {EncodePowerShellCommand(command)}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8
        };
    }

    private static ProcessStartInfo CreateOneCoreProcessStartInfo(string text, TtsSpeechOptions options)
    {
        var command = BuildOneCorePlaybackCommand(text, options);
        return new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-Sta -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {EncodePowerShellCommand(command)}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8
        };
    }

    private static string BuildSapiVoiceSelectionCommand(TtsSpeechOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.VoiceId))
        {
            return string.Empty;
        }

        var voiceId = EscapePowerShellSingleQuotedString(options.VoiceId);
        return $"$targetVoiceId = '{voiceId}'; " +
            "foreach ($voice in $speaker.GetVoices()) { if ($voice.Id -eq $targetVoiceId) { $speaker.Voice = $voice; break } }; ";
    }

    private static string BuildSapiRateCommand(TtsSpeechOptions options)
    {
        if (options.UseSystemSpeechSettings)
        {
            return string.Empty;
        }

        var sapiRate = Math.Clamp((int)Math.Round(options.ReadingSpeedDelta * 4.0), -10, 10);
        return $"$speaker.Rate = {sapiRate}; ";
    }

    private static string BuildOneCoreProbeCommand() =>
        "$ErrorActionPreference = 'Stop'; " +
        "Add-Type -AssemblyName System.Runtime.WindowsRuntime; " +
        "$null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Foundation, ContentType = WindowsRuntime]; " +
        "$synth = [Activator]::CreateInstance([Type]::GetType('Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows, ContentType=WindowsRuntime')); " +
        "$voiceCount = @([Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices).Count; " +
        "[Console]::Out.Write(('OneCore voices=' + $voiceCount));";

    private static string BuildOneCorePlaybackCommand(string text, TtsSpeechOptions options)
    {
        var escapedVoiceId = EscapePowerShellSingleQuotedString(options.VoiceId ?? string.Empty);
        var escapedLanguageCode = EscapePowerShellSingleQuotedString(options.LanguageCode);
        var ssmlPayload = EscapePowerShellSingleQuotedString(BuildSsmlForOneCore(text, options));

        return
            "$ErrorActionPreference = 'Stop'; " +
            "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); " +
            "Add-Type -AssemblyName System.Runtime.WindowsRuntime; " +
            "$null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Foundation, ContentType = WindowsRuntime]; " +
            "$null = [Windows.Media.SpeechSynthesis.SpeechSynthesisStream, Windows.Foundation, ContentType = WindowsRuntime]; " +
            "$null = [Windows.Storage.Streams.DataReader, Windows.Foundation, ContentType = WindowsRuntime]; " +
            "$null = [Windows.Foundation.IAsyncOperation`1, Windows.Foundation, ContentType = WindowsRuntime]; " +
            "function Await($Operation, [Type] $ResultType) { " +
                "$asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | " +
                    "Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 } | " +
                    "Select-Object -First 1; " +
                "$task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation)); " +
                "$task.GetAwaiter().GetResult() " +
            "}; " +
            "$synth = [Activator]::CreateInstance([Type]::GetType('Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows, ContentType=WindowsRuntime')); " +
            "$targetVoiceId = '" + escapedVoiceId + "'; " +
            "if (-not [string]::IsNullOrWhiteSpace($targetVoiceId)) { " +
                "$voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices | Where-Object { $_.Id -eq $targetVoiceId } | Select-Object -First 1; " +
                "if ($null -ne $voice) { $synth.Voice = $voice } " +
            "} " +
            "$ssml = '" + ssmlPayload + "'; " +
            "$stream = Await ($synth.SynthesizeSsmlToStreamAsync($ssml)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream]); " +
            "$wavePath = Join-Path $env:TEMP ('wordsuggestor-onecore-' + [guid]::NewGuid().ToString('N') + '.wav'); " +
            "try { " +
                "$reader = New-Object Windows.Storage.Streams.DataReader($stream.GetInputStreamAt(0)); " +
                "try { " +
                    "$size = [uint32]$stream.Size; " +
                    "$null = Await ($reader.LoadAsync($size)) ([uint32]); " +
                    "$bytes = New-Object byte[] ([int]$stream.Size); " +
                    "$reader.ReadBytes($bytes); " +
                    "[System.IO.File]::WriteAllBytes($wavePath, $bytes); " +
                    "$player = New-Object System.Media.SoundPlayer $wavePath; " +
                    "$player.PlaySync(); " +
                    "[Console]::Out.Write('OneCore playback completed.'); " +
                "} finally { " +
                    "$reader.Dispose(); " +
                "} " +
            "} finally { " +
                "if ($stream -is [System.IDisposable]) { $stream.Dispose() }; " +
                "Remove-Item -LiteralPath $wavePath -Force -ErrorAction SilentlyContinue; " +
            "}";
    }

    private static string BuildSsmlForOneCore(string text, TtsSpeechOptions options)
    {
        var escapedText = EscapeXmlText(text);
        if (options.UseSystemSpeechSettings)
        {
            return
                $"<speak version=\"1.0\" xml:lang=\"{options.LanguageCode}\" xmlns=\"http://www.w3.org/2001/10/synthesis\">" +
                escapedText +
                "</speak>";
        }

        var ratePercent = Math.Clamp((int)Math.Round(options.ReadingSpeedDelta * 30.0), -60, 60);
        var rateToken = ratePercent >= 0 ? $"+{ratePercent}%" : $"{ratePercent}%";
        return
            $"<speak version=\"1.0\" xml:lang=\"{options.LanguageCode}\" xmlns=\"http://www.w3.org/2001/10/synthesis\">" +
            $"<prosody rate=\"{rateToken}\">{escapedText}</prosody>" +
            "</speak>";
    }

    private static string EscapeXmlText(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);

    private static string? CombineFallbackReasons(params string?[] values)
    {
        var reasons = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return reasons.Length == 0 ? null : string.Join(" ", reasons);
    }

    private static string EscapePowerShellSingleQuotedString(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private static string EncodePowerShellCommand(string command) =>
        Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

    private static void WriteDiagnostic(string message)
    {
        var line = $"{DateTimeOffset.Now:O} {message}";
        Debug.WriteLine($"WordSuggestor TTS: {message}");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DiagnosticLogPath)!);
            File.AppendAllText(DiagnosticLogPath, line + Environment.NewLine);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string TrimForDiagnostic(string value)
    {
        var singleLine = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        return singleLine.Length <= 500 ? singleLine : singleLine[..500];
    }

    private sealed record InvocationPlan(
        ProcessStartInfo StartInfo,
        string Backend,
        string VoiceDisplayName,
        string VoiceSource,
        string? FallbackReason);
}

public sealed record OneCoreRuntimeStatus(bool IsAvailable, string Detail);

public sealed record TextToSpeechInvocationResult(
    string Backend,
    string VoiceDisplayName,
    string VoiceSource,
    string? FallbackReason);

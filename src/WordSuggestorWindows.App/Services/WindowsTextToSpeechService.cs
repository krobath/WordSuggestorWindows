using System.Diagnostics;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.Json;
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
    private readonly StringBuilder _stdoutBuffer = new();
    private readonly StringBuilder _stderrBuffer = new();
    private readonly List<string> _temporaryArtifactPaths = [];

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? SpeechStopped;

    public event EventHandler? PrecisePlaybackStarted;

    public event EventHandler<TextToSpeechBoundaryCue>? BoundaryCueReceived;

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

        _stdoutBuffer.Clear();
        _stderrBuffer.Clear();
        _temporaryArtifactPaths.Clear();
        _temporaryArtifactPaths.AddRange(invocation.TemporaryArtifactPaths ?? Array.Empty<string>());

        try
        {
            _process = Process.Start(invocation.StartInfo)
                ?? throw new InvalidOperationException("Windows TTS bridge kunne ikke startes.");
        }
        catch
        {
            CleanupTemporaryArtifacts();
            throw;
        }
        _process.EnableRaisingEvents = true;
        _process.Exited += ProcessOnExited;
        _process.OutputDataReceived += ProcessOnOutputDataReceived;
        _process.ErrorDataReceived += ProcessOnErrorDataReceived;
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        StatusChanged?.Invoke(
            this,
            invocation.FallbackReason is null
                ? $"Oplæsning startet med {invocation.VoiceDisplayName} ({text.Length} tegn)."
                : $"Oplæsning bruger fallback: {invocation.FallbackReason}");
        return new TextToSpeechInvocationResult(
            invocation.Backend,
            invocation.VoiceDisplayName,
            invocation.VoiceSource,
            invocation.FallbackReason,
            invocation.SupportsPreciseBoundaryMetadata);
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
                var oneCoreInvocation = CreateOneCoreProcessStartInfo(text, options);
                return new InvocationPlan(
                    oneCoreInvocation.StartInfo,
                    WindowsVoiceCatalogService.OneCoreSource,
                    options.VoiceDisplayName ?? "Windows OneCore-stemme",
                    WindowsVoiceCatalogService.OneCoreSource,
                    options.FallbackReason,
                    !string.Equals(options.ReadingHighlightMode, "none", StringComparison.OrdinalIgnoreCase),
                    oneCoreInvocation.TemporaryArtifactPaths);
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
                var stderr = _stderrBuffer.ToString();
                var stdout = _stdoutBuffer.ToString();
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

    private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        _stdoutBuffer.AppendLine(e.Data);

        if (string.Equals(e.Data, "WS_TTS|PLAYBACK_START", StringComparison.Ordinal))
        {
            PrecisePlaybackStarted?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (TryParseBoundaryCue(e.Data, out var cue))
        {
            BoundaryCueReceived?.Invoke(this, cue);
        }
    }

    private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        _stderrBuffer.AppendLine(e.Data);
    }

    private void CleanupProcess()
    {
        if (_process is null)
        {
            CleanupTemporaryArtifacts();
            return;
        }

        _process.Exited -= ProcessOnExited;
        _process.OutputDataReceived -= ProcessOnOutputDataReceived;
        _process.ErrorDataReceived -= ProcessOnErrorDataReceived;
        _process.Dispose();
        _process = null;
        CleanupTemporaryArtifacts();
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

    private static OneCoreProcessInvocation CreateOneCoreProcessStartInfo(string text, TtsSpeechOptions options)
    {
        var artifactPrefix = Path.Combine(
            Path.GetTempPath(),
            $"wordsuggestor-onecore-{Guid.NewGuid():N}");
        var payloadPath = artifactPrefix + ".json";
        var scriptPath = artifactPrefix + ".ps1";
        var payload = new OneCorePlaybackPayload(
            options.VoiceId,
            BuildSsmlForOneCore(text, options),
            string.Equals(options.ReadingHighlightMode, "word", StringComparison.OrdinalIgnoreCase),
            string.Equals(options.ReadingHighlightMode, "sentence", StringComparison.OrdinalIgnoreCase));

        File.WriteAllText(
            payloadPath,
            JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
            new UTF8Encoding(false));
        File.WriteAllText(
            scriptPath,
            BuildOneCorePlaybackScript(),
            new UTF8Encoding(false));

        return new OneCoreProcessInvocation(
            new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-Sta -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{scriptPath}\" -PayloadPath \"{payloadPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            },
            [payloadPath, scriptPath]);
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

    private static string BuildOneCorePlaybackScript() =>
        """
param(
    [Parameter(Mandatory = $true)]
    [string] $PayloadPath
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Media.SpeechSynthesis.SpeechSynthesisStream, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Media.Core.SpeechCue, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.DataReader, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Foundation.IAsyncOperation`1, Windows.Foundation, ContentType = WindowsRuntime]

function Await($Operation, [Type] $ResultType) {
    $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() |
        Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 } |
        Select-Object -First 1
    $task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
    $task.GetAwaiter().GetResult()
}

function Emit([string] $line) {
    [Console]::Out.WriteLine($line)
    [Console]::Out.Flush()
}

$payload = Get-Content -LiteralPath $PayloadPath -Raw -Encoding UTF8 | ConvertFrom-Json
$synth = [Activator]::CreateInstance([Type]::GetType('Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows, ContentType=WindowsRuntime'))
$synth.Options.IncludeWordBoundaryMetadata = [bool]$payload.includeWordBoundaryMetadata
$synth.Options.IncludeSentenceBoundaryMetadata = [bool]$payload.includeSentenceBoundaryMetadata

$targetVoiceId = [string]$payload.voiceId
if (-not [string]::IsNullOrWhiteSpace($targetVoiceId)) {
    $voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices | Where-Object { $_.Id -eq $targetVoiceId } | Select-Object -First 1
    if ($null -ne $voice) {
        $synth.Voice = $voice
    }
}

$ssml = [string]$payload.ssml
$stream = Await ($synth.SynthesizeSsmlToStreamAsync($ssml)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
$wavePath = Join-Path $env:TEMP ('wordsuggestor-onecore-' + [guid]::NewGuid().ToString('N') + '.wav')

try {
    foreach ($track in $stream.TimedMetadataTracks) {
        foreach ($cueObject in $track.Cues) {
            $cue = [Windows.Media.Core.SpeechCue]$cueObject
            if ($null -eq $cue) {
                continue
            }

            $startPosition = if ($null -ne $cue.StartPositionInInput) { [int]$cue.StartPositionInInput } else { -1 }
            $length = 0
            if (-not [string]::IsNullOrWhiteSpace($cue.Text)) {
                $length = $cue.Text.Length
            } elseif ($null -ne $cue.EndPositionInInput -and $startPosition -ge 0) {
                $length = [Math]::Max(0, ([int]$cue.EndPositionInInput) - $startPosition)
            }

            if ($startPosition -ge 0 -and $length -gt 0) {
                $startMs = [int][Math]::Round($cue.StartTime.TotalMilliseconds)
                $durationMs = [int][Math]::Round($cue.Duration.TotalMilliseconds)
                Emit('WS_TTS|CUE|' + $startMs + '|' + $durationMs + '|' + $startPosition + '|' + $length)
            }
        }
    }

    $reader = New-Object Windows.Storage.Streams.DataReader($stream.GetInputStreamAt(0))
    try {
        $size = [uint32]$stream.Size
        $null = Await ($reader.LoadAsync($size)) ([uint32])
        $bytes = New-Object byte[] ([int]$stream.Size)
        $reader.ReadBytes($bytes)
        [System.IO.File]::WriteAllBytes($wavePath, $bytes)
        $player = New-Object System.Media.SoundPlayer $wavePath
        $player.Load()
        Emit('WS_TTS|PLAYBACK_START')
        $player.PlaySync()
        Emit('WS_TTS|PLAYBACK_END')
    } finally {
        $reader.Dispose()
    }
} finally {
    if ($stream -is [System.IDisposable]) { $stream.Dispose() }
    Remove-Item -LiteralPath $wavePath -Force -ErrorAction SilentlyContinue
}
""";

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

    private static bool TryParseBoundaryCue(string line, out TextToSpeechBoundaryCue cue)
    {
        cue = default!;

        var parts = line.Split('|');
        if (parts.Length != 6 ||
            !string.Equals(parts[0], "WS_TTS", StringComparison.Ordinal) ||
            !string.Equals(parts[1], "CUE", StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var startMs) ||
            !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var durationMs) ||
            !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var start) ||
            !int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var length))
        {
            return false;
        }

        cue = new TextToSpeechBoundaryCue(
            TimeSpan.FromMilliseconds(Math.Max(0, startMs)),
            TimeSpan.FromMilliseconds(Math.Max(0, durationMs)),
            Math.Max(0, start),
            Math.Max(0, length));
        return length > 0;
    }

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

    private void CleanupTemporaryArtifacts()
    {
        foreach (var path in _temporaryArtifactPaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        _temporaryArtifactPaths.Clear();
    }

    private sealed record InvocationPlan(
        ProcessStartInfo StartInfo,
        string Backend,
        string VoiceDisplayName,
        string VoiceSource,
        string? FallbackReason,
        bool SupportsPreciseBoundaryMetadata = false,
        IReadOnlyList<string>? TemporaryArtifactPaths = null);

    private sealed record OneCorePlaybackPayload(
        string? VoiceId,
        string Ssml,
        bool IncludeWordBoundaryMetadata,
        bool IncludeSentenceBoundaryMetadata);

    private sealed record OneCoreProcessInvocation(
        ProcessStartInfo StartInfo,
        IReadOnlyList<string> TemporaryArtifactPaths);
}

public sealed record OneCoreRuntimeStatus(bool IsAvailable, string Detail);

public sealed record TextToSpeechInvocationResult(
    string Backend,
    string VoiceDisplayName,
    string VoiceSource,
    string? FallbackReason,
    bool SupportsPreciseBoundaryMetadata = false);

public sealed record TextToSpeechBoundaryCue(
    TimeSpan StartTime,
    TimeSpan Duration,
    int Start,
    int Length);

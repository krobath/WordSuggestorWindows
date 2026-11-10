using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// Event sender is an opaque utterance identity. Dispatcher clients recheck it
// after posting; old process events can never describe the replacement process.
public sealed class WindowsTextToSpeechService : IDisposable
{
    private readonly object _gate = new();
    private readonly Func<ProcessStartInfo>? _bridgeFactory;
    private readonly TimeSpan _timeout;
    private Session? _current;
    private static readonly Lazy<OneCoreRuntimeStatus> OneCoreStatus = new(ProbeOneCore);
    public WindowsTextToSpeechService(Func<ProcessStartInfo>? bridgeFactory = null, TimeSpan? timeout = null)
    { _bridgeFactory = bridgeFactory; _timeout = timeout ?? TimeSpan.FromMinutes(20); }
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? SpeechStopped;
    public event EventHandler? PrecisePlaybackStarted;
    public event EventHandler<TextToSpeechBoundaryCue>? BoundaryCueReceived;
    public bool IsSpeaking { get { lock (_gate) return _current is { Active: true }; } }
    public bool IsCurrent(object? sender) { lock (_gate) return sender is not null && ReferenceEquals(sender, _current); }
    internal object? CurrentIdentity { get { lock (_gate) return _current; } }
    public int? CurrentProcessId { get { lock (_gate) return _current is { Active: true } s ? s.Process.Id : null; } }
    public TimeSpan? LastAudioDuration { get { lock (_gate) return _current?.AudioDuration; } }
    public static OneCoreRuntimeStatus GetOneCoreRuntimeStatus() => OneCoreStatus.Value;

    public TextToSpeechInvocationResult Speak(string text, TtsSpeechOptions options)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 16000) throw new InvalidOperationException("Ingen eller for meget tekst til oplæsning.");
        lock (_gate)
        {
            Stop();
            var backend = options.VoiceSource == WindowsVoiceCatalogService.OneCoreSource ? WindowsVoiceCatalogService.OneCoreSource : WindowsVoiceCatalogService.SapiDesktopSource;
            if (_bridgeFactory is null && backend == WindowsVoiceCatalogService.OneCoreSource && !GetOneCoreRuntimeStatus().IsAvailable)
            {
                var fallback = WindowsVoiceCatalogService.ResolveVoiceBySource(options.LanguageCode, null, WindowsVoiceCatalogService.SapiDesktopSource);
                if (fallback.Voice is null) throw new InvalidOperationException("Ingen tilgængelig Windows-talestemme.");
                options = options with { VoiceId = fallback.Voice.Id, VoiceDisplayName = fallback.Voice.DisplayName, VoiceSource = fallback.Voice.Source,
                    FallbackReason = "OneCore er utilgængelig; Windows SAPI bruges." };
                backend = WindowsVoiceCatalogService.SapiDesktopSource;
            }
            var directory = Path.Combine(Path.GetTempPath(), "wordsuggestor-speech-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var session = new Session(directory, text, options.ReadingHighlightMode, backend);
            try
            {
                var scriptPath = Path.Combine(directory, "worker.ps1");
                using (var resource = typeof(WindowsTextToSpeechService).Assembly.GetManifestResourceStream("WordSuggestorWindows.SpeechWorker")!)
                using (var script = File.Create(scriptPath)) resource.CopyTo(script);
                var info = _bridgeFactory?.Invoke() ?? new ProcessStartInfo(PowerShellPath);
                if (_bridgeFactory is null)
                    foreach (var arg in new[] { "-Sta", "-NoProfile", "-NonInteractive", "-WindowStyle", "Hidden", "-ExecutionPolicy", "Bypass", "-File", scriptPath }) info.ArgumentList.Add(arg);
                info.UseShellExecute = false; info.CreateNoWindow = true;
                info.RedirectStandardInput = info.RedirectStandardOutput = info.RedirectStandardError = true;
                info.StandardInputEncoding = new UTF8Encoding(false); info.StandardOutputEncoding = info.StandardErrorEncoding = Encoding.UTF8;
                session.Process = Process.Start(info) ?? throw new InvalidOperationException();
                _current = session;
                var payload = JsonSerializer.Serialize(new { version = 1, text, backend, voiceId = options.VoiceId, voiceName = options.VoiceDisplayName,
                    useSystem = options.UseSystemSpeechSettings, multiplier = SpeechSpeedPolicy.Multiplier(options.ReadingSpeedDelta), highlight = options.ReadingHighlightMode,
                    wavePath = Path.Combine(directory, "speech.wav") });
                _ = ObserveAsync(session, payload);
                StatusChanged?.Invoke(session, "Oplæsning forberedes.");
                return new(backend, options.VoiceDisplayName ?? "Windows standardstemme", backend, options.FallbackReason, options.ReadingHighlightMode != "none");
            }
            catch
            {
                _current = null; KillAndReap(session); Cleanup(session);
                throw new InvalidOperationException("Windows-oplæsning kunne ikke starte.");
            }
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            var session = _current; _current = null;
            if (session is null) return;
            session.Active = false; session.Cancel.Cancel(); KillAndReap(session); Cleanup(session);
        }
    }
    public void Dispose() => Stop();

    private async Task ObserveAsync(Session session, string payload)
    {
        var status = "Oplæsning fejlede. Prøv en anden Windows-stemme.";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(session.Cancel.Token);
        timeout.CancelAfter(_timeout);
        async Task Guard(Func<Task> action) { try { await action(); } catch { timeout.Cancel(); throw; } }
        try
        {
            await Task.WhenAll(
                Guard(() => DrainAsync(session.Process.StandardError, timeout.Token)),
                Guard(() => ReadOutputAsync(session, timeout.Token)),
                Guard(async () => { await session.Process.StandardInput.WriteAsync(payload.AsMemory(), timeout.Token); session.Process.StandardInput.Close(); }),
                Guard(() => session.Process.WaitForExitAsync(timeout.Token)));
            if (session.Process.ExitCode == 0 && session.PlaybackStarted) status = "Oplæsning færdig.";
        }
        catch (OperationCanceledException) { status = "Oplæsning afbrudt."; }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
        finally
        {
            lock (_gate)
            {
                session.Active = false; KillAndReap(session); Cleanup(session);
                if (ReferenceEquals(_current, session)) SpeechStopped?.Invoke(session, status);
            }
        }
    }
    private async Task ReadOutputAsync(Session session, CancellationToken token)
    {
        var buffer = new char[1024]; var line = new StringBuilder(); var total = 0;
        int read;
        while ((read = await session.Process.StandardOutput.ReadAsync(buffer.AsMemory(), token)) > 0)
        {
            total += read; if (total > 2 * 1024 * 1024) throw new InvalidOperationException();
            for (var i = 0; i < read; i++)
            {
                var character = buffer[i];
                if (character == '\n') { HandleLine(session, line.ToString().TrimEnd('\r')); line.Clear(); }
                else { if (line.Length >= 512) throw new InvalidOperationException(); line.Append(character); }
            }
        }
        if (line.Length > 0) HandleLine(session, line.ToString());
    }
    private void HandleLine(Session session, string line)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_current, session) || !session.Active) return;
            if (line == "WS_TTS|PLAYBACK_START") { session.PlaybackStarted = true; PrecisePlaybackStarted?.Invoke(session, EventArgs.Empty); return; }
            var parts = line.Split('|');
            if (parts.Length == 3 && parts[0] == "WS_TTS" && parts[1] == "AUDIO" && int.TryParse(parts[2], out var milliseconds) && milliseconds > 0)
            { session.AudioDuration = TimeSpan.FromMilliseconds(milliseconds); return; }
            if (parts.Length != 6 || parts[0] != "WS_TTS" || parts[1] != "CUE" ||
                !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var at) ||
                !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var duration) ||
                !int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var start) ||
                !int.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out var length) ||
                !ValidRange(session.Text, start, length)) return;
            if (session.Mode == "none") return;
            if (session.Mode == "sentence" && session.Backend != WindowsVoiceCatalogService.OneCoreSource && TypingReadAloudPolicy.SentenceAt(session.Text, start) is { } sentence)
            { start = sentence.Start; length = sentence.Length; }
            BoundaryCueReceived?.Invoke(session, new(TimeSpan.FromMilliseconds(at), TimeSpan.FromMilliseconds(duration), start, length));
        }
    }
    public static bool ValidRange(string text, int start, int length) => start >= 0 && length > 0 && start <= text.Length - length &&
        Boundary(text, start) && Boundary(text, start + length);
    private static bool Boundary(string text, int at) => at == 0 || at == text.Length || !(char.IsLowSurrogate(text[at]) && char.IsHighSurrogate(text[at - 1]));
    private static async Task DrainAsync(StreamReader reader, CancellationToken token)
    {
        var buffer = new char[1024]; var count = 0; int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), token)) > 0) { count += read; if (count > 256 * 1024) throw new InvalidOperationException(); }
    }
    private static void KillAndReap(Session session)
    {
        try { if (!session.Process.HasExited) session.Process.Kill(entireProcessTree: true); session.Process.WaitForExit(2000); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }
    private static void Cleanup(Session session)
    {
        foreach (var name in new[] { "worker.ps1", "speech.wav" })
            try { File.Delete(Path.Combine(session.Directory, name)); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        try { Directory.Delete(session.Directory); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        session.Process.Dispose();
    }
    private static OneCoreRuntimeStatus ProbeOneCore()
    {
        var command = "$ErrorActionPreference='Stop'; Add-Type -AssemblyName System.Runtime.WindowsRuntime; $null=[Windows.Media.SpeechSynthesis.SpeechSynthesizer,Windows.Foundation,ContentType=WindowsRuntime]; $s=[Activator]::CreateInstance([Type]::GetType('Windows.Media.SpeechSynthesis.SpeechSynthesizer,Windows,ContentType=WindowsRuntime')); $s.Dispose()";
        var info = new ProcessStartInfo(PowerShellPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-WindowStyle", "Hidden", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(command)) }) info.ArgumentList.Add(arg);
        try
        {
            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(5000)) { process.Kill(entireProcessTree: true); process.WaitForExit(); return new(false, "OneCore-kontrol fik timeout."); }
            return new(process.ExitCode == 0, process.ExitCode == 0 ? "OneCore tilgængelig." : "OneCore utilgængelig.");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { return new(false, "OneCore utilgængelig."); }
    }
    private static string PowerShellPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
    private sealed class Session(string directory, string text, string mode, string backend)
    {
        public readonly string Directory = directory, Text = text, Mode = mode, Backend = backend;
        public Process Process = new(); public readonly CancellationTokenSource Cancel = new();
        public bool Active = true, PlaybackStarted; public TimeSpan? AudioDuration;
    }
}
public sealed record OneCoreRuntimeStatus(bool IsAvailable, string Detail);
public sealed record TextToSpeechInvocationResult(string Backend, string VoiceDisplayName, string VoiceSource, string? FallbackReason, bool SupportsPreciseBoundaryMetadata = false);
public sealed record TextToSpeechBoundaryCue(TimeSpan StartTime, TimeSpan Duration, int Start, int Length);

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

    private Process? _process;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? SpeechStopped;

    public bool IsSpeaking => _process is { HasExited: false };

    public void Speak(string text, TtsSpeechOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            WriteDiagnostic("Speak skipped: empty text.");
            return;
        }

        Stop();
        WriteDiagnostic(
            $"Speak requested: chars={text.Trim().Length}, lang={options.LanguageCode}, voice={options.VoiceDisplayName ?? "default"}, voiceIdPresent={!string.IsNullOrWhiteSpace(options.VoiceId)}, useSystemSettings={options.UseSystemSpeechSettings}, speedDelta={options.ReadingSpeedDelta}, fallback={options.FallbackReason ?? "none"}.");

        var command = "$ErrorActionPreference = 'Stop'; " +
            "$speaker = New-Object -ComObject SAPI.SpVoice; " +
            BuildVoiceSelectionCommand(options) +
            BuildRateCommand(options) +
            $"$null = $speaker.Speak('{EscapePowerShellSingleQuotedString(text)}')";

        var startInfo = new ProcessStartInfo
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

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows TTS bridge kunne ikke startes.");
        _process.EnableRaisingEvents = true;
        _process.Exited += ProcessOnExited;
        StatusChanged?.Invoke(
            this,
            options.FallbackReason is null
                ? $"Oplæsning startet med {options.VoiceDisplayName ?? "Windows standardstemme"} ({text.Length} tegn)."
                : $"Oplæsning bruger fallback: {options.FallbackReason}");
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

    private static string BuildVoiceSelectionCommand(TtsSpeechOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.VoiceId))
        {
            return string.Empty;
        }

        var voiceId = EscapePowerShellSingleQuotedString(options.VoiceId);
        return $"$targetVoiceId = '{voiceId}'; " +
            "foreach ($voice in $speaker.GetVoices()) { if ($voice.Id -eq $targetVoiceId) { $speaker.Voice = $voice; break } }; ";
    }

    private static string BuildRateCommand(TtsSpeechOptions options)
    {
        if (options.UseSystemSpeechSettings)
        {
            return string.Empty;
        }

        var sapiRate = Math.Clamp((int)Math.Round(options.ReadingSpeedDelta * 4.0), -10, 10);
        return $"$speaker.Rate = {sapiRate}; ";
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
}

using System.Diagnostics;
using System.Text;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsTextToSpeechService : IDisposable
{
    private Process? _process;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? SpeechStopped;

    public bool IsSpeaking => _process is { HasExited: false };

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Stop();
        var command = "$ErrorActionPreference = 'Stop'; " +
            "$speaker = New-Object -ComObject SAPI.SpVoice; " +
            $"$null = $speaker.Speak('{EscapePowerShellSingleQuotedString(text)}')";

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {EncodePowerShellCommand(command)}",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows TTS bridge kunne ikke startes.");
        _process.EnableRaisingEvents = true;
        _process.Exited += ProcessOnExited;
        StatusChanged?.Invoke(this, $"Oplæsning startet ({text.Length} tegn).");
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
        CleanupProcess();
        SpeechStopped?.Invoke(this, "Oplæsning stoppet.");
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

    private static string EscapePowerShellSingleQuotedString(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private static string EncodePowerShellCommand(string command) =>
        Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
}

using System.Diagnostics;
using System.Text;

namespace WordSuggestorWindows.App.Services;

public sealed class OverlaySpeechService : IDisposable
{
    public void Speak(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return;
        }

        var command = "$speaker = New-Object -ComObject SAPI.SpVoice; " +
                      $"$null = $speaker.Speak('{EscapePowerShellSingleQuotedString(term)}')";

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {EncodePowerShellCommand(command)}",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        Process.Start(startInfo);
    }

    public void Dispose()
    {
    }

    private static string EscapePowerShellSingleQuotedString(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private static string EncodePowerShellCommand(string command) =>
        Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
}

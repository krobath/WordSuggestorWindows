using System.Diagnostics;
using System.IO;
using System.Text;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsSpeechToTextService : IDisposable
{
    private const string StopCommand = "STOP";
    private Process? _process;
    private string? _scriptPath;
    private string? _lastErrorStatus;

    private const string SpeechBridgeScript = """
        param(
            [Parameter(Mandatory = $true)]
            [string] $LanguageCode,

            [Parameter(Mandatory = $true)]
            [string] $DisplayName
        )

        $ErrorActionPreference = 'Stop'
        [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
        [Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)

        Add-Type -AssemblyName System.Speech

        function Write-BridgeLine([string] $Kind, [string] $Culture, [double] $Confidence, [string] $Text) {
            $encodedText = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($Text))
            [Console]::Out.WriteLine(('{0}`t{1}`t{2}`t{3}' -f $Kind, $Culture, $Confidence.ToString([System.Globalization.CultureInfo]::InvariantCulture), $encodedText))
            [Console]::Out.Flush()
        }

        $recognizers = @([System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers())
        if ($recognizers.Count -eq 0) {
            [Console]::Error.WriteLine('No Windows Speech Recognition recognizers are installed.')
            exit 30
        }

        $requestedCulture = [System.Globalization.CultureInfo]::GetCultureInfo($LanguageCode)
        $recognizer = $recognizers |
            Where-Object { $_.Culture.Name -ieq $requestedCulture.Name } |
            Select-Object -First 1
        if ($null -eq $recognizer) {
            $recognizer = $recognizers |
                Where-Object { $_.Culture.TwoLetterISOLanguageName -ieq $requestedCulture.TwoLetterISOLanguageName } |
                Select-Object -First 1
        }
        if ($null -eq $recognizer) {
            $recognizer = $recognizers | Select-Object -First 1
        }

        $engine = [System.Speech.Recognition.SpeechRecognitionEngine]::new($recognizer)
        $recognizerCultureName = $recognizer.Culture.Name
        try {
            $engine.LoadGrammar([System.Speech.Recognition.DictationGrammar]::new())
            $engine.SetInputToDefaultAudioDevice()

            $engine.add_SpeechRecognized({
                param($Sender, $EventArgs)
                $text = $EventArgs.Result.Text
                if (-not [string]::IsNullOrWhiteSpace($text) -and $EventArgs.Result.Confidence -ge 0.25) {
                    Write-BridgeLine 'FINAL' $recognizerCultureName $EventArgs.Result.Confidence $text.Trim()
                }
            })

            $engine.add_SpeechHypothesized({
                param($Sender, $EventArgs)
                $text = $EventArgs.Result.Text
                if (-not [string]::IsNullOrWhiteSpace($text)) {
                    Write-BridgeLine 'HYP' $recognizerCultureName $EventArgs.Result.Confidence $text.Trim()
                }
            })

            Write-BridgeLine 'READY' $recognizer.Culture.Name 1.0 $recognizer.Description
            $engine.RecognizeAsync([System.Speech.Recognition.RecognizeMode]::Multiple)

            while ($true) {
                $line = [Console]::In.ReadLine()
                if ($null -eq $line -or $line -eq 'STOP') {
                    break
                }
            }

            $engine.RecognizeAsyncCancel()
        }
        finally {
            $engine.Dispose()
        }
        """;

    public event EventHandler<SpeechToTextTranscript>? TranscriptReceived;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? SessionStopped;

    public bool IsListening => _process is { HasExited: false };

    public string Start(LanguageOption requestedLanguage)
    {
        if (IsListening)
        {
            return "Tale-til-tekst lytter allerede.";
        }

        _scriptPath = Path.Combine(Path.GetTempPath(), $"wordsuggestor-speech-bridge-{Guid.NewGuid():N}.ps1");
        _lastErrorStatus = null;
        File.WriteAllText(_scriptPath, SpeechBridgeScript, Encoding.UTF8);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(_scriptPath);
        startInfo.ArgumentList.Add("-LanguageCode");
        startInfo.ArgumentList.Add(requestedLanguage.LanguageCode);
        startInfo.ArgumentList.Add("-DisplayName");
        startInfo.ArgumentList.Add(requestedLanguage.DisplayName);

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("PowerShell speech bridge kunne ikke startes.");
        _process.EnableRaisingEvents = true;
        _process.Exited += ProcessOnExited;
        _ = Task.Run(() => ReadStandardOutputAsync(_process));
        _ = Task.Run(() => ReadStandardErrorAsync(_process));

        return $"Tale-til-tekst starter for {requestedLanguage.DisplayName}.";
    }

    public void Stop()
    {
        if (!IsListening || _process is null)
        {
            CleanupProcess();
            return;
        }

        try
        {
            _process.StandardInput.WriteLine(StopCommand);
            _process.StandardInput.Flush();
        }
        catch (InvalidOperationException)
        {
            CleanupProcess();
        }
        catch (IOException)
        {
            CleanupProcess();
        }
    }

    public void Dispose()
    {
        Stop();
        CleanupProcess();
    }

    private async Task ReadStandardOutputAsync(Process process)
    {
        try
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                HandleBridgeLine(line);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private async Task ReadStandardErrorAsync(Process process)
    {
        try
        {
            var error = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(error))
            {
                _lastErrorStatus = $"Tale-til-tekst bridge: {SanitizeForStatus(error)}";
                StatusChanged?.Invoke(this, _lastErrorStatus);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void HandleBridgeLine(string line)
    {
        var parts = line.Split('\t', 4);
        if (parts.Length != 4)
        {
            return;
        }

        var text = DecodeBridgeText(parts[3]);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (parts[0] == "READY")
        {
            StatusChanged?.Invoke(this, $"Tale-til-tekst lytter via {text} ({parts[1]}).");
            return;
        }

        var confidence = double.TryParse(
            parts[2],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsedConfidence)
                ? parsedConfidence
                : 0;

        TranscriptReceived?.Invoke(
            this,
            new SpeechToTextTranscript(
                text,
                confidence,
                parts[1],
                IsFinal: parts[0] == "FINAL"));
    }

    private void ProcessOnExited(object? sender, EventArgs e)
    {
        var message = _lastErrorStatus ?? "Tale-til-tekst er stoppet.";
        CleanupProcess();
        SessionStopped?.Invoke(this, message);
    }

    private void CleanupProcess()
    {
        if (_process is not null)
        {
            _process.Exited -= ProcessOnExited;
            _process.Dispose();
            _process = null;
        }

        if (_scriptPath is null)
        {
            return;
        }

        try
        {
            File.Delete(_scriptPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        _scriptPath = null;
    }

    private static string DecodeBridgeText(string encoded)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    private static string SanitizeForStatus(string value)
    {
        var sanitized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        return sanitized.Length <= 240 ? sanitized : sanitized[..240] + "...";
    }
}

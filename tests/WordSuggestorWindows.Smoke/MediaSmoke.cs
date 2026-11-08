using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class MediaSmoke
{
    public static async Task Run(string repo)
    {
        var data = Path.Combine(repo, ".artifacts", "tests", "media-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(data);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", data);
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, 500, 140));
            context.DrawText(new FormattedText("Hej OCR test", CultureInfo.GetCultureInfo("da-DK"),
                FlowDirection.LeftToRight, new Typeface("Arial"), 36, Brushes.Black, 1), new Point(20, 40));
        }
        var bitmap = new RenderTargetBitmap(500, 140, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(drawing);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var image = Path.Combine(data, "ocr-fixture.png");
        using (var stream = File.Create(image)) encoder.Save(stream);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var recognized = await new WindowsOcrService().RecognizeImageAsync(image, timeout.Token);
        if (recognized is null || !recognized.Contains("OCR test", StringComparison.OrdinalIgnoreCase)) throw new Exception("OCR adapter did not recognize synthetic fixture.");
        Console.WriteLine("PASS MEDIA actual Windows OCR production adapter recognizes synthetic PNG");

        // Exercise the actual encoder/receiver with a synthetic wire fixture;
        // this does not record a microphone or claim acoustic recognition.
        var script = WindowsSpeechToTextService.SpeechBridgeScript;
        script = script[..script.IndexOf("$recognizers =", StringComparison.Ordinal)] +
            "\nWrite-BridgeLine 'FINAL' 'da-DK' 0.9 'Prøve æøå'\n";
        var scriptPath = Path.Combine(data, "speech-wire.ps1");
        File.WriteAllText(scriptPath, script, new UTF8Encoding(true));
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", scriptPath, "-LanguageCode", "da-DK", "-DisplayName", "Test" }) start.ArgumentList.Add(arg);
        using var child = Process.Start(start)!;
        var output = child.StandardOutput.ReadToEndAsync(timeout.Token);
        var errors = child.StandardError.ReadToEndAsync(timeout.Token);
        await child.WaitForExitAsync(timeout.Token);
        var line = (await output).Trim(); _ = await errors;
        SpeechToTextTranscript? transcript = null;
        using var stt = new WindowsSpeechToTextService();
        stt.TranscriptReceived += (_, value) => transcript = value;
        stt.HandleBridgeLine(line);
        if (child.ExitCode != 0 || transcript is not { IsFinal: true, Text: "Prøve æøå", CultureName: "da-DK" })
            throw new Exception("STT production encoder/receiver contract failed.");
        Console.WriteLine("PASS MEDIA STT PowerShell/C# UTF-8 and real-tab contract; microphone not recorded");

        var voices = WindowsVoiceCatalogService.GetInstalledVoices();
        var voice = voices.FirstOrDefault(v => v.LanguageCode == "da-DK") ?? voices.FirstOrDefault()
            ?? throw new Exception("No installed TTS voice.");
        using var tts = new WindowsTextToSpeechService();
        var finished = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        tts.SpeechStopped += (_, status) => finished.TrySetResult(status);
        var invocation = tts.Speak("Hej test.", new TtsSpeechOptions(voice.LanguageCode, voice.Id,
            voice.DisplayName, voice.Source, "none", true, 0, null));
        var completion = await finished.Task.WaitAsync(TimeSpan.FromSeconds(30));
        if (!completion.Contains("færdig", StringComparison.OrdinalIgnoreCase)) throw new Exception("TTS playback process failed.");
        Console.WriteLine($"PASS MEDIA actual TTS process completed; backend={invocation.Backend}; voice={invocation.VoiceDisplayName}. Audible quality not human-verified.");
        Console.WriteLine("PASS MEDIA 3 adapter/contract checks. Full microphone/snipping/highlight tests remain manual.");
    }
}

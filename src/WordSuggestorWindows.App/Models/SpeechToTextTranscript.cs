namespace WordSuggestorWindows.App.Models;

public sealed record SpeechToTextTranscript(
    string Text,
    double Confidence,
    string CultureName,
    bool IsFinal);

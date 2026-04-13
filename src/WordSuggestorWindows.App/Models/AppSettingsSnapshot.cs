namespace WordSuggestorWindows.App.Models;

public sealed class AppSettingsSnapshot
{
    public int SchemaVersion { get; set; } = 1;

    public bool IsGlobalCaptureEnabled { get; set; } = true;

    public string SelectedLanguageCode { get; set; } = "da-DK";

    public string SuggestionPlacementMode { get; set; } = "followCaret";

    public bool IsTextAnalyzerEnabled { get; set; } = true;

    public bool IsTextAnalyzerColoringEnabled { get; set; } = true;

    public bool IsSemanticDiagnosticsEnabled { get; set; }

    public bool IsPunctuationDiagnosticsEnabled { get; set; }

    public string PunctuationCommaStyle { get; set; } = "Standard";

    public bool IsErrorTrackingEnabled { get; set; } = true;

    public bool StoreSentenceExamples { get; set; }

    public bool IsDomainListsEnabled { get; set; }

    public bool UseSystemSpeechSettings { get; set; } = true;

    public string TextToSpeechVoiceMode { get; set; } = "Automatisk";

    public string SpeechLanguageMode { get; set; } = "useSelectedLanguage";

    public double ReadingSpeedDelta { get; set; }

    public string ReadingStrategy { get; set; } = "none";

    public string ReadingHighlightMode { get; set; } = "word";

    public Dictionary<string, string> SystemVoiceIdOverrideByLanguage { get; set; } = [];

    public bool IsPerformanceInstrumentationEnabled { get; set; }

    public bool IsAppDebugLoggingEnabled { get; set; }

    public bool IsPlacementDebugLoggingEnabled { get; set; }

    public bool IsCoreDebugLoggingEnabled { get; set; }

    public AppSettingsSnapshot Clone() =>
        new()
        {
            SchemaVersion = SchemaVersion,
            IsGlobalCaptureEnabled = IsGlobalCaptureEnabled,
            SelectedLanguageCode = SelectedLanguageCode,
            SuggestionPlacementMode = SuggestionPlacementMode,
            IsTextAnalyzerEnabled = IsTextAnalyzerEnabled,
            IsTextAnalyzerColoringEnabled = IsTextAnalyzerColoringEnabled,
            IsSemanticDiagnosticsEnabled = IsSemanticDiagnosticsEnabled,
            IsPunctuationDiagnosticsEnabled = IsPunctuationDiagnosticsEnabled,
            PunctuationCommaStyle = PunctuationCommaStyle,
            IsErrorTrackingEnabled = IsErrorTrackingEnabled,
            StoreSentenceExamples = StoreSentenceExamples,
            IsDomainListsEnabled = IsDomainListsEnabled,
            UseSystemSpeechSettings = UseSystemSpeechSettings,
            TextToSpeechVoiceMode = TextToSpeechVoiceMode,
            SpeechLanguageMode = SpeechLanguageMode,
            ReadingSpeedDelta = ReadingSpeedDelta,
            ReadingStrategy = ReadingStrategy,
            ReadingHighlightMode = ReadingHighlightMode,
            SystemVoiceIdOverrideByLanguage = new Dictionary<string, string>(
                SystemVoiceIdOverrideByLanguage,
                StringComparer.OrdinalIgnoreCase),
            IsPerformanceInstrumentationEnabled = IsPerformanceInstrumentationEnabled,
            IsAppDebugLoggingEnabled = IsAppDebugLoggingEnabled,
            IsPlacementDebugLoggingEnabled = IsPlacementDebugLoggingEnabled,
            IsCoreDebugLoggingEnabled = IsCoreDebugLoggingEnabled
        };
}

namespace WordSuggestorWindows.App.Services;

// Presentation/playback policy only; no lexical/ranking logic.
public static class SpeechSpeedPolicy
{
    public static double Normalize(double delta) => double.IsFinite(delta)
        ? Math.Round(Math.Clamp(delta, -.5, .5) / .05, MidpointRounding.AwayFromZero) * .05 : 0;
    public static double Multiplier(double delta) => 1 + Normalize(delta);
    public static int Percentage(double delta) => (int)Math.Round(Multiplier(delta) * 100);
    // Old OneCore used 1.35^delta after the effective -1..1 load clamp.
    // Preserve its requested tempo to the nearest new 5% step, once only.
    public static double FromLegacy(double delta) => Normalize(Math.Pow(1.35, Math.Clamp(delta, -1, 1)) - 1);
}

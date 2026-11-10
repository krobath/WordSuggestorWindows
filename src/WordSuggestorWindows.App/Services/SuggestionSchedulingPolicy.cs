namespace WordSuggestorWindows.App.Services;

// Scheduling only: ranking and source selection remain in Core. A rapid typing
// streak gets a quiet window; cancelled requests cannot queue behind the engine.
public sealed class SuggestionSchedulingPolicy
{
    private static readonly int[] Delay = [20, 35, 55, 85, 110];
    private static readonly int[] Quiet = [140, 200, 260, 380, 500];
    private static readonly int[] Cadence = [75, 95, 110, 140, 160];
    private static readonly int[] Streak = [5, 4, 3, 3, 2];
    private long? _last;
    private int _streak;
    public int NextDelay(int profile, long now)
    {
        profile = Math.Clamp(profile, 0, 4);
        _streak = _last is { } last && now - last >= 0 && now - last < Cadence[profile] ? _streak + 1 : 0;
        _last = now;
        return _streak >= Streak[profile] ? Quiet[profile] : Delay[profile];
    }
}

using System.Text;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class UnknownWordImportService(ICoreUnknownWordAdapter core, WindowsAppSettingsService settings, LearnedWordStore store)
{
    public async Task<string> ImportAsync(string word, CoreRequestContext scope, Func<bool> current, CancellationToken token)
    {
        bool Allowed() => current() && settings.Load() is { IsUnknownWordLookupEnabled: true, UnknownWordLookupConsentVersion: 1 };
        if (!Allowed()) return "consent_required";
        var lookup = await core.LookupUnknownWordAsync(word, scope, 1, token);
        if (!Allowed()) throw new OperationCanceledException();
        if (lookup.Context != scope) throw new InvalidOperationException("Core scope rejected.");
        if (lookup.Value.Status != "resolved") return lookup.Value.Status;
        using var lease = await store.AcquireAsync(token);
        store.CheckExisting();
        var fingerprint = store.Fingerprint();
        var prepared = await core.PrepareLearnedWordAsync(word, lookup.Value.Proposal!.Value, scope, 1, token);
        if (!Allowed()) throw new OperationCanceledException();
        if (prepared.Context != scope || prepared.Language != lookup.Language) throw new InvalidOperationException("Core scope rejected.");
        if (prepared.Value.Status != "prepared") return prepared.Value.Status;
        await store.CommitAsync(Encoding.UTF8.GetBytes(prepared.Value.Snapshot!), () => Allowed() && store.Fingerprint() == fingerprint, token);
        return "saved";
    }
}

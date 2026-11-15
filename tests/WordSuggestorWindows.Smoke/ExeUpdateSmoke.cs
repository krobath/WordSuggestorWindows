using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WordSuggestorWindows.App.Services;

internal static class ExeUpdateSmoke
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(respond(r)); } }
    public static async Task Run(string repo)
    {
        var count = 0;
        void Check(string label, bool pass) { if (!pass) throw new Exception("EXE update: " + label); count++; Console.WriteLine("PASS EXE " + label); }
        var root = Path.Combine(repo, ".artifacts", "tests", "exe-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var installed = new DirectInstallation("WordSuggestor.Windows.Beta.Direct", "0.2.0.1", "0.2.0-beta.1", "CN=Fixture", true);
        var bytes = Encoding.UTF8.GetBytes("synthetic installer; never executed");
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string Feed(string version = "0.2.0.2", string? hash = null, long? size = null) => JsonSerializer.Serialize(new { schema_version = 1, platform = "windows", architecture = "x64", channel = "beta", package_version = version, version = "0.2.0-beta.2", filename = "WordSuggestor.Beta_" + version + "_x64.exe", size_bytes = size ?? bytes.Length, sha256 = hash ?? sha, publisher = installed.Publisher });
        WindowsExeUpdateService Service(string body, byte[]? payload = null, bool trusted = true, Action<string>? launch = null, HttpStatusCode status = HttpStatusCode.OK, bool versionMatches = true) => new(installed,
            new Handler(r => new(r.RequestUri!.AbsoluteUri == WindowsExeUpdateService.FeedUrl ? status : HttpStatusCode.OK) { Content = r.RequestUri!.AbsoluteUri == WindowsExeUpdateService.FeedUrl ? new StringContent(body) : new ByteArrayContent(payload ?? bytes) }),
            (_, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(trusted); }, launch ?? (_ => throw new Exception("Unexpected launch.")), Path.Combine(root, Guid.NewGuid().ToString("N")), (_, _) => versionMatches);
        foreach (var pair in new[] { ("0.2.0.2", "available"), ("0.2.0.1", "current"), ("0.2.0.0", "current") })
        { using var service = Service(Feed(pair.Item1)); Check("monotonic version " + pair.Item1, (await service.CheckAsync(default)).Code == pair.Item2); }
        foreach (var body in new[] { "{broken", Feed().Replace("CN=Fixture", "CN=Other"), Feed().Replace("\"x64\"", "\"arm64\""), Feed().Replace("WordSuggestor.Beta_", "../WordSuggestor.Beta_"), Feed().Replace("\"beta\"", "\"stable\""), Feed(hash: new string('A', 64)), Feed(size: 0), Feed(size: 1073741825), Feed("99999.0.0.1"), Feed().TrimEnd('}') + ",\"url\":\"https://evil.example\"}", new string('x', 32769) })
        { using var service = Service(body); Check("invalid metadata rejected", (await service.CheckAsync(default)).Code == "invalid_feed"); }
        using (var service = Service("", status: HttpStatusCode.NotFound)) Check("unpublished feed calm", (await service.CheckAsync(default)).Code == "unpublished");
        using (var service = Service("", status: HttpStatusCode.Redirect)) Check("redirect not accepted", (await service.CheckAsync(default)).Code == "unavailable");
        using (var service = Service(Feed().TrimEnd('}') + ",\"platform\":\"windows\"}")) Check("duplicate metadata field rejected", (await service.CheckAsync(default)).Code == "invalid_feed");
        using (var service = new WindowsExeUpdateService(installed with { ProductId = "WordSuggestor.Windows.Preview.123456abcdef", UpdatesEnabled = false }, new Handler(_ => throw new Exception("Preview network access.")))) Check("preview never checks public feed", (await service.CheckAsync(default)).Code == "local_preview");
        using (var service = Service(Feed()))
        {
            using var cts = new CancellationTokenSource(); cts.Cancel(); var rejected = false;
            try { await service.CheckAsync(cts.Token); } catch (OperationCanceledException) { rejected = true; }
            Check("cancellation preserved", rejected);
        }
        foreach (var scenario in new[] { "hash", "size", "trust", "version" })
        {
            using var service = Service(Feed(), scenario == "hash" ? Encoding.UTF8.GetBytes(new string('x', bytes.Length)) : scenario == "size" ? [1, 2] : bytes, scenario != "trust", versionMatches: scenario != "version");
            var rejected = false;
            try { await service.DownloadVerifiedAsync((await service.CheckAsync(default)).Offer!, default); } catch (InvalidDataException) { rejected = true; }
            Check("bad installer rejected: " + scenario, rejected);
        }
        var launched = 0;
        var feedReads = 0;
        using (var service = new WindowsExeUpdateService(installed, new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent(Feed(++feedReads == 1 ? "0.2.0.2" : "0.2.0.3")) })))
        {
            var offer = (await service.CheckAsync(default)).Offer!; var rejected = false;
            try { await service.DownloadVerifiedAsync(offer, default); } catch (InvalidDataException) { rejected = true; }
            Check("changed server offer requires a new confirmation", rejected && feedReads == 2);
        }
        using (var service = Service(Feed(), launch: _ => launched++))
        using (var updates = new WindowsUpdateCoordinator(direct: service))
        {
            await updates.RefreshAsync(); await updates.CheckAsync(); Check("EXE offer independent of G05", updates.IsDirectInstalled && updates.HasOffer && !updates.PolicyState.CanChange);
            var ticket = await updates.PrepareHandoffAsync(); Check("download does not execute", ticket is not null && launched == 0);
            Check("single-use explicit handoff", ticket!.TryOpen() && !ticket.TryOpen() && launched == 1);
        }
        feedReads = 0;
        using (var service = new WindowsExeUpdateService(installed, new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent(Feed(++feedReads == 1 ? "0.2.0.2" : "0.2.0.3")) })))
        using (var updates = new WindowsUpdateCoordinator(direct: service))
        {
            await updates.CheckAsync();
            Check("coordinator does not replace the user-confirmed version", await updates.PrepareHandoffAsync() is null && updates.StatusCode == "offer_changed" && feedReads == 2);
        }
        string? cached = null;
        var trustedNow = true;
        using (var service = new WindowsExeUpdateService(installed, new Handler(r => new(HttpStatusCode.OK) { Content = r.RequestUri!.AbsoluteUri == WindowsExeUpdateService.FeedUrl ? new StringContent(Feed()) : new ByteArrayContent(bytes) }), (_, _, _) => Task.FromResult(trustedNow), _ => launched++, root, (_, _) => true))
        {
            var launcher = await service.DownloadVerifiedAsync((await service.CheckAsync(default)).Offer!, default);
            trustedNow = false; var rejected = false;
            try { launcher.Open(); } catch (InvalidOperationException) { rejected = true; }
            Check("trust is reverified immediately before launch", rejected && launched == 1);
        }
        using (var service = new WindowsExeUpdateService(installed, new Handler(r => new(HttpStatusCode.OK) { Content = r.RequestUri!.AbsoluteUri == WindowsExeUpdateService.FeedUrl ? new StringContent(Feed()) : new ByteArrayContent(bytes) }), (p, _, _) => { cached = p; return Task.FromResult(true); }, _ => launched++, root, (_, _) => true))
        {
            var launcher = await service.DownloadVerifiedAsync((await service.CheckAsync(default)).Offer!, default);
            File.WriteAllText(cached!, "tampered"); var rejected = false;
            try { launcher.Open(); } catch (InvalidOperationException) { rejected = true; }
            Check("changed cached installer cannot launch", rejected && launched == 1);
        }
        var marker = Path.Combine(root, "direct-install.json");
        File.WriteAllText(marker, JsonSerializer.Serialize(new { schema_version = 1, product_id = "WordSuggestor.Windows.Preview.123456abcdef", package_version = "0.2.0.1", display_version = "0.2.0-beta.1", publisher = "CN=Fixture", updates_enabled = false }));
        Check("preview identity isolated", DirectInstallation.Read(root)?.PreviewId == "123456abcdef");
        File.WriteAllText(marker, File.ReadAllText(marker).Replace("false", "true")); Check("preview cannot enable public updates", DirectInstallation.Read(root) is null);
        var store = new WindowsAppSettingsService(Path.Combine(root, "settings.json")); var before = store.Load(); var edited = before.Clone(); edited.CheckExeUpdatesAutomatically = false; store.SaveChanges(before, edited);
        Check("check preference survives restart", !store.Load().Clone().CheckExeUpdatesAutomatically);
        var vendor = Path.Combine(repo, ".artifacts", "tools", "inno-7.1.1", "compiler-setup.exe");
        if (File.Exists(vendor))
        {
            // The hash-pinned vendor installer has textual ProductVersion 7.1.1
            // but leaves numeric FileVersion at 0.0.0.0. Read the numeric fields.
            Check("native PE numeric fields read accurately", WindowsExeUpdateService.VerifyInstallerVersion(vendor, new Version(0, 0, 0, 0)));
            Check("textual product version cannot substitute for numeric file version", !WindowsExeUpdateService.VerifyInstallerVersion(vendor, new Version(7, 1, 1, 0)));
            Check("real native Authenticode valid", await WindowsExeUpdateService.VerifyTrustAsync(vendor, "CN=Pyrsys B.V., O=Pyrsys B.V., S=Noord-Holland, C=NL", default));
            Check("native publisher mismatch rejected", !await WindowsExeUpdateService.VerifyTrustAsync(vendor, "CN=Other", default));
            Check("native unsigned payload rejected", !await WindowsExeUpdateService.VerifyTrustAsync(cached!, installed.Publisher, default));
        }
        else Console.WriteLine("NOT RUN EXE native signature: provision pinned compiler.");
        Console.WriteLine("PASS EXE " + count + " checks; synthetic launch, real native trust reads where noted.");
    }
}

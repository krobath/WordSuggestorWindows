using System.IO;
using System.Net;
using System.Net.Http;
using WordSuggestorWindows.App.Services;

internal static class UpdateSmoke
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { token.ThrowIfCancellationRequested(); return Task.FromResult(response(request)); }
    }

    public static async Task<int> Run()
    {
        var passed = 0;
        var installed = new InstalledRelease(WindowsUpdateService.PackageName, "CN=Fixture", new Version(0, 2, 0, 1));
        string Feed(string version) => $"""
            <AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2018" Version="{version}" Uri="{WindowsUpdateService.FeedUrl}">
              <MainPackage Name="WordSuggestor.Windows.Beta" Publisher="CN=Fixture" Version="{version}" ProcessorArchitecture="x64"
                Uri="https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta_0.2.0.2_x64.msix" />
            </AppInstaller>
            """;
        async Task Check(string label, string code, string xml, HttpStatusCode status = HttpStatusCode.OK)
        {
            using var service = new WindowsUpdateService(new Handler(_ => new(status) { Content = new StringContent(xml) }), installed);
            var result = await service.CheckAsync();
            if (result.Code != code) throw new Exception($"Update gate {label}: {result.Code}");
            passed++; Console.WriteLine("PASS UPDATE " + label);
        }
        await Check("new version offered", "available", Feed("0.2.0.2"));
        await Check("same version stays current", "current", Feed("0.2.0.1"));
        await Check("older feed does not downgrade", "current", Feed("0.2.0.0"));
        await Check("wrong identity rejected", "invalid_feed", Feed("0.2.0.2").Replace("Name=\"WordSuggestor.Windows.Beta\"", "Name=\"Other.App\""));
        await Check("wrong publisher rejected", "invalid_feed", Feed("0.2.0.2").Replace("CN=Fixture", "CN=Other"));
        await Check("off-origin package rejected", "invalid_feed", Feed("0.2.0.2").Replace("/WordSuggestor.Beta_", "@evil.example/WordSuggestor.Beta_"));
        await Check("wrong architecture rejected", "invalid_feed", Feed("0.2.0.2").Replace("x64\"", "arm64\""));
        await Check("malformed feed is calm", "invalid_feed", "<bad");
        await Check("DTD rejected", "invalid_feed", "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///sensitive'>]>" + Feed("0.2.0.2"));
        await Check("missing first feed", "unpublished", "", HttpStatusCode.NotFound);
        await Check("server error is calm", "unavailable", "", HttpStatusCode.ServiceUnavailable);
        await Check("oversized metadata rejected", "invalid_feed", new string('x', 131073));
        using var offline = new WindowsUpdateService(new Handler(_ => throw new HttpRequestException("PRIVATE_ERROR_DETAIL")), installed);
        var offlineResult = await offline.CheckAsync();
        if (offlineResult.Code != "offline" || offlineResult.Message.Contains("PRIVATE_ERROR_DETAIL")) throw new Exception("Offline update privacy gate");
        passed++; Console.WriteLine("PASS UPDATE offline does not expose process/network detail");
        return passed;
    }
}

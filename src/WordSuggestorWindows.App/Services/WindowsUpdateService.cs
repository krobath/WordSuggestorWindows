using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;

namespace WordSuggestorWindows.App.Services;

public sealed record InstalledRelease(string Name, string Publisher, Version Version);
public sealed record UpdateCheckResult(string Code, string Message, Version? AvailableVersion = null);

// App Installer owns verification, installation and automatic scheduling. This
// adapter only checks bounded public metadata and offers the OS-owned installer.
public sealed class WindowsUpdateService : IDisposable
{
    public const string PackageName = "WordSuggestor.Windows.Beta";
    public const string FeedUrl = "https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta.appinstaller";
    private const string ArtifactBase = "https://wordsuggestor.com/beta-artifacts/windows/x64/beta/";
    private readonly HttpClient _http;
    private readonly InstalledRelease? _installed;

    public WindowsUpdateService(HttpMessageHandler? handler = null, InstalledRelease? installed = null)
    {
        _http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
            { Timeout = TimeSpan.FromSeconds(8) };
        _installed = installed ?? ReadInstalledRelease();
    }

    public static string DisplayVersion => Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "ukendt";
    public string VersionLabel => _installed is null
        ? $"WordSuggestor {DisplayVersion} · lokal udviklerudgave"
        : $"WordSuggestor {DisplayVersion} · pakke {_installed.Version} · x64";
    public bool IsProductionInstalled => _installed?.Name == PackageName;

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsProductionInstalled) return new("not_installed",
            "Automatiske opdateringer kræver den installerede Windows-beta. Lokale testudgaver bruger deres separate testfeed.");
        try
        {
            using var response = await _http.GetAsync(FeedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return new("unpublished", "Der er endnu ikke offentliggjort et Windows-beta-feed.");
            if (!response.IsSuccessStatusCode) return new("unavailable", "Opdateringer kunne ikke kontrolleres. Prøv igen senere.");
            if (response.Content.Headers.ContentLength is > 131072) return new("invalid_feed", "Opdateringsoplysningerne kunne ikke valideres.");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[4096];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) != 0)
            {
                if (buffer.Length + read > 131072) return new("invalid_feed", "Opdateringsoplysningerne kunne ikke valideres.");
                buffer.Write(chunk, 0, read);
            }
            buffer.Position = 0;
            return ValidateFeed(buffer, _installed!);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return new("offline", "Ingen forbindelse til opdateringsserveren. Du kan fortsætte med at skrive offline."); }
        catch (HttpRequestException)
        { return new("offline", "Ingen forbindelse til opdateringsserveren. Du kan fortsætte med at skrive offline."); }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException or ArgumentException or IOException)
        { return new("invalid_feed", "Opdateringsoplysningerne kunne ikke valideres."); }
    }

    public static UpdateCheckResult ValidateFeed(Stream stream, InstalledRelease installed)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 131072 });
        var document = XDocument.Load(reader);
        XNamespace ns = "http://schemas.microsoft.com/appx/appinstaller/2018";
        var root = document.Root;
        var package = root?.Element(ns + "MainPackage");
        var uri = (string?)package?.Attribute("Uri");
        if (root?.Name != ns + "AppInstaller" || root.Elements(ns + "MainPackage").Count() != 1 ||
            root.Element(ns + "MainBundle") is not null || (string?)root.Attribute("Uri") != FeedUrl ||
            (string?)package?.Attribute("Name") != installed.Name ||
            (string?)package?.Attribute("Publisher") != installed.Publisher ||
            (string?)package?.Attribute("ProcessorArchitecture") != "x64" ||
            uri is null || !uri.StartsWith(ArtifactBase, StringComparison.Ordinal) ||
            !System.Text.RegularExpressions.Regex.IsMatch(uri[ArtifactBase.Length..], @"^[A-Za-z0-9][A-Za-z0-9._-]*\.msix$") ||
            uri.Contains("..", StringComparison.Ordinal) ||
            !Version.TryParse((string?)package?.Attribute("Version"), out var version) || version.Revision < 0 ||
            version.Major > 65535 || version.Minor > 65535 || version.Build > 65535 || version.Revision > 65535 ||
            (string?)root.Attribute("Version") != version.ToString())
            return new("invalid_feed", "Opdateringens identitet, udgiver eller adresse kunne ikke valideres.");
        return version > installed.Version
            ? new("available", $"Version {version} er klar. Gem dit arbejde og afslut WordSuggestor, før du installerer opdateringen.", version)
            : new("current", "Du har den nyeste tilgængelige Windows-beta.");
    }

    public void OpenInstaller()
    {
        if (!IsProductionInstalled) throw new InvalidOperationException("Installed production beta required.");
        Process.Start(new ProcessStartInfo(FeedUrl) { UseShellExecute = true });
    }

    public static InstalledRelease? ReadInstalledRelease()
    {
        uint size = 0;
        if (GetCurrentPackageFullName(ref size, null) != 122) return null; // APPMODEL_ERROR_NO_PACKAGE for loose builds
        try
        {
            using var input = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "AppxManifest.xml"));
            using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var root = XDocument.Load(reader).Root;
            var identity = root?.Elements().SingleOrDefault(e => e.Name.LocalName == "Identity");
            return identity is null ? null : new((string)identity.Attribute("Name")!, (string)identity.Attribute("Publisher")!,
                Version.Parse((string)identity.Attribute("Version")!));
        }
        catch (Exception ex) when (ex is IOException or XmlException or ArgumentException or InvalidOperationException) { return null; }
    }

    public void Dispose() => _http.Dispose();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint length, char[]? packageFullName);
}

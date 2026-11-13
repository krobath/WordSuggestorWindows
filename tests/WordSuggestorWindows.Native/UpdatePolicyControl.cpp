// Isolated MSIX diagnostic only. Never operates on a package other than itself.
#include <windows.h>
#include <winrt/Windows.ApplicationModel.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Management.Deployment.h>
#include <array>
#include <iostream>
#include <iomanip>
#include <optional>
#include <regex>
#include <tuple>
#include <vector>
using namespace winrt;
using namespace Windows::ApplicationModel;
using namespace Windows::Management::Deployment;

static auto Fingerprint(AppInstallerInfo const& info) {
    auto uris = [](auto const& values) { std::vector<std::wstring> out; for (auto const& uri : values) out.emplace_back(uri.AbsoluteUri()); return out; };
    auto version = info.Version();
    return std::tuple(std::wstring(info.Uri().AbsoluteUri()), std::array{version.Major,version.Minor,version.Build,version.Revision},
        info.HoursBetweenUpdateChecks(), info.ShowPrompt(), info.UpdateBlocksActivation(), info.ForceUpdateFromAnyVersion(),
        info.IsAutoRepairEnabled(), info.PolicySource(), uris(info.UpdateUris()), uris(info.RepairUris()),
        uris(info.OptionalPackageUris()), uris(info.DependencyPackageUris()));
}

static AutoUpdateSettingsOptions Options(AppInstallerInfo const& info, bool manual) {
    if (!manual) return AutoUpdateSettingsOptions::CreateFromAppInstallerInfo(info);
    AutoUpdateSettingsOptions out;
    out.AppInstallerUri(Windows::Foundation::Uri(info.Uri().AbsoluteUri())); out.Version(info.Version());
    out.OnLaunch(info.OnLaunch()); out.AutomaticBackgroundTask(info.AutomaticBackgroundTask());
    out.HoursBetweenUpdateChecks(info.HoursBetweenUpdateChecks()); out.ShowPrompt(info.ShowPrompt());
    out.UpdateBlocksActivation(info.UpdateBlocksActivation()); out.ForceUpdateFromAnyVersion(info.ForceUpdateFromAnyVersion()); out.IsAutoRepairEnabled(info.IsAutoRepairEnabled());
    for (auto const& uri : info.UpdateUris()) out.UpdateUris().Append(Windows::Foundation::Uri(uri.AbsoluteUri()));
    for (auto const& uri : info.RepairUris()) out.RepairUris().Append(Windows::Foundation::Uri(uri.AbsoluteUri()));
    for (auto const& uri : info.OptionalPackageUris()) out.OptionalPackageUris().Append(Windows::Foundation::Uri(uri.AbsoluteUri()));
    for (auto const& uri : info.DependencyPackageUris()) out.DependencyPackageUris().Append(Windows::Foundation::Uri(uri.AbsoluteUri()));
    return out;
}

int wmain(int argc, wchar_t** argv) {
    char const* stage = "identity"; char const* failureStage = "none";
    HRESULT error = S_OK, factoryError = S_OK; bool restored = true, noop = false, off = false, on = false, context = false, manual = false;
    AppInstallerManager manager{nullptr}; AutoUpdateSettingsOptions restore{nullptr};
    Package package{nullptr}; AppInstallerInfo original{nullptr}; bool attempted = false;
    std::optional<decltype(Fingerprint(original))> originalFingerprint;
    bool originalOn = false, originalBackground = false;
    try {
        init_apartment(apartment_type::multi_threaded);
        if (argc != 2) throw hresult_invalid_argument();
        package = Package::Current();
        auto id = package.Id(); auto name = std::wstring(id.Name());
        std::wregex allowed(L"^WordSuggestor\\.Windows\\.LocalTest\\.([a-f0-9]{12})$"); std::wsmatch match;
        if (!std::regex_match(name,match,allowed) || id.Publisher() != hstring(L"CN=WordSuggestor Local Test " + match[1].str()) || id.FamilyName() != argv[1]) throw hresult_access_denied();
        HANDLE token{}; TOKEN_ELEVATION elevation{}; DWORD size{};
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &token)) throw hresult_access_denied();
        auto queried = GetTokenInformation(token, TokenElevation, &elevation, sizeof(elevation), &size); CloseHandle(token);
        if (!queried || elevation.TokenIsElevated) throw hresult_access_denied();
        context = true;
        auto read = [&] { return package.GetAppInstallerInfo(); };
        stage = "read_policy"; original = read(); auto fingerprint = Fingerprint(original);
        originalFingerprint = fingerprint; originalOn = original.OnLaunch(); originalBackground = original.AutomaticBackgroundTask();
        stage = "create_options"; restore = AutoUpdateSettingsOptions::CreateFromAppInstallerInfo(original);
        manager = AppInstallerManager::GetDefault();
        // A no-op distinguishes failure to transmit original options from a
        // failure specific to disabling schedules while retaining other flags.
        stage = "create_factory_noop"; auto factoryNoop = Options(original,false);
        stage = "write_noop"; attempted = true;
        try { manager.SetAutoUpdateSettings(id.FamilyName(), factoryNoop); }
        catch (hresult_error const& ex) {
            factoryError = ex.code();
            auto unchanged = read();
            if (Fingerprint(unchanged) != fingerprint || unchanged.OnLaunch() != originalOn || unchanged.AutomaticBackgroundTask() != originalBackground) throw;
            // Compare a newly constructed native object, without the factory or
            // managed projection. Retain exactly the same visible policy data.
            manual = true; stage = "create_manual_noop";
            restore = Options(original,true);
            auto manualNoop = Options(original,true);
            stage = "write_manual_noop";
            manager.SetAutoUpdateSettings(id.FamilyName(), manualNoop);
        }
        auto now = read(); noop = Fingerprint(now) == fingerprint && now.OnLaunch() == original.OnLaunch() && now.AutomaticBackgroundTask() == original.AutomaticBackgroundTask();
        if (!noop) throw hresult_error(E_FAIL);
        for (bool enabled : {false,true}) {
            stage = enabled ? "write_on" : "write_off";
            auto options = Options(original,manual);
            options.OnLaunch(enabled); options.AutomaticBackgroundTask(enabled);
            manager.SetAutoUpdateSettings(id.FamilyName(),options);
            now = read();
            if (Fingerprint(now) != fingerprint || now.OnLaunch() != enabled || now.AutomaticBackgroundTask() != enabled) throw hresult_error(E_FAIL);
            if (enabled) on = true; else off = true;
        }
    } catch (hresult_error const& ex) { error = ex.code(); failureStage = stage; }
      catch (...) { error = E_FAIL; failureStage = stage; }
    if (attempted && original && manager && restore) {
        try {
            auto read = [&] { return package.GetAppInstallerInfo(); };
            auto now = read();
            if (Fingerprint(now) != *originalFingerprint || now.OnLaunch() != originalOn || now.AutomaticBackgroundTask() != originalBackground)
                manager.SetAutoUpdateSettings(package.Id().FamilyName(),restore);
            now = read();
            restored = Fingerprint(now) == *originalFingerprint && now.OnLaunch() == originalOn && now.AutomaticBackgroundTask() == originalBackground;
        } catch (...) { restored = false; }
    }
    auto flag = [](bool value) { return value ? "true" : "false"; };
    std::cout << "{\"schema_version\":2,\"info_route\":\"current_package\",\"packaged_context_matches\":" << flag(context)
        << ",\"noop_verified\":" << flag(noop) << ",\"off_verified\":" << flag(off) << ",\"on_verified\":" << flag(on)
        << ",\"restored\":" << flag(restored) << ",\"failure_stage\":\"" << failureStage
        << "\",\"hresult\":\"0x" << std::hex << std::uppercase << std::setw(8) << std::setfill('0') << static_cast<unsigned long>(error)
        << "\",\"factory_hresult\":\"0x" << std::setw(8) << static_cast<unsigned long>(factoryError) << "\",\"manual_options\":" << flag(manual) << "}" << std::endl;
    return !restored ? 1 : SUCCEEDED(error) && off && on ? 0 : 3;
}

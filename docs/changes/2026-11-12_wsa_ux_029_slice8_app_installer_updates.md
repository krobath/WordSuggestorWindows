# WSA-UX-029 slice 8 — App Installer Settings candidate

## Summary

Implement actual Windows update-state display and safe manual update orchestration.
Retain WPF/Core/pack, existing MSIX release model and unrelated working-tree work.
Native packaged policy mutation remains unqualified; editing stays read-only.

## User-visible behavior

General shows installed/development version and actual OS automatic-check flags,
including unknown/mixed states. Manual check does not install. A separate offer
requires explicit confirmation and normal save/close before opening App Installer.
Unavailable, offline, invalid feed and OS disagreement have calm DA/EN explanations.
The read-only editing gate does not disable existing Windows automatic scheduling.

## Implementation details

Typed WinRT adapter, immutable snapshots, serialized change/readback/restore path,
single-flight coordinator and expiring one-use handoff isolate Windows authority.
Package identity comes from the running OS context. Bounded canonical HTTPS XML
validation rejects related payloads, downgrade/activation-blocking policy and
ambiguous versions. Reacquire the package for OS availability checks. Use normal
owner/root close and existing dirty-document guards. Pin managed Windows SDK
10.0.22621.57 and compile TFM 22621, retain API minimum 19041 and script output
paths. Core revision, language-pack format and backend O-052 remain unchanged.

## Files changed

- `/README.md`
- `/dependencies.lock.json`
- `/docs/Architecture.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/Release.md`
- `/docs/UiParityPlan.md`
- `/docs/WindowsUpdates.md`
- `/docs/changes/2026-11-12_wsa_ux_029_slice8_app_installer_updates.md`
- `/docs/qualification/WSA-UX-029_slice8.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_settings_app.ps1`
- `/scripts/test_settings_platform.ps1`
- `/src/WordSuggestorWindows.App/App.xaml.cs`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/Models/WindowsUpdateContracts.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.Updates.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsNativeUpdatePlatform.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateCoordinator.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateHandoff.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateQualification.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateService.cs`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/WordSuggestorWindows.App.csproj`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsUiSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/UpdatePackageActivation.cs`
- `/tests/WordSuggestorWindows.Smoke/UpdateSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/WordSuggestorWindows.Smoke.csproj`

## Feature flags / settings impact

No automatic-update bool/schema migration is added to user settings. Production
mutation is source-gated false until native packaged acceptance, with no config/
environment override. The explicit strict LocalTest activation cannot authorize
production writes. Hello hardware acceptance is user-deferred on 2026-11-12.

## Logging / telemetry impact

Status codes and HRESULTs replace raw OS/HTTP exception output. Native reports
contain fixture identity, version, binary digest and change/restoration flags;
no document text, tokens, passwords or keys. No telemetry/backend changes.

## Validation performed

Fresh exact-source archives: build 0 C# warnings/errors; Core 335 + 6 Danish
fixtures; app 67; UpdateState 26, AccountState 44, AnalysisState 30,
SuggestionState 27, Speech 102, Core adapter 58, State 63 and safety 35 PASS.
Earlier active-desktop candidate passed 37 update checks. All/GUI were not run
in the final archive because the session is disconnected; the earlier isolated
All attempt timed out waiting for the physical input target. No fresh desktop
signoff is claimed; Media exited 1 on a black OCR fixture, reproduced with the prior
slice-7 baseline in the same session. No expectation was weakened. Rerun desktop/
media gates when the session is active. Actual packaged mutation remains BLOCKED
(prerequisite mode exit 3); HelloNative was not rerun. See
[full results, source fingerprint and manual matrix](../qualification/WSA-UX-029_slice8.md).
Final review covers 34 documented files, original mixed hunks and 18 untracked
file hashes, secrets, binaries, machine paths and local links. No all-green
software suite, positive native mutation or release qualification is claimed.

## Known limitations / follow-up

In-app automatic policy editing remains read-only. Developer registration was
blocked by OS policy; an isolated signed leaf was not trusted as a root. Temporary
test trust/key material was removed; no root trust, machine policy or production
package changed. Trusted fixture provisioning, native pause/system policy behavior,
Windows 10, real N→N+1/signature rejection/relaunch and human accessibility remain
open. No official package/feed/publication is qualified by this slice.
Next slice builds Advanced diagnostic controls without collecting document text;
release acceptance and user-deferred Hello remain separate.

## Consolidation notes for `WordSuggestorDocs`

Later consolidation should link the Windows-local update contract, distinguish
actual OS state from app preferences and keep native mutation/install gates open.
Record the user's Hello deferral. Shared Docs/backend/macOS/Core are unchanged;
no live implementation docs are moved across platform ownership boundaries.

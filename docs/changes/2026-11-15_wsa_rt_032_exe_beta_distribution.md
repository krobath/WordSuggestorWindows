# WSA-RT-032: conventional Windows beta installer and confirmed updates

## Summary

At the user's request, park G05/App Installer policy work and provide a normal
per-user EXE installer plus user-confirmed updates. Preserve the current WPF
application, Core CLI, SQLite pack contract, editor/Settings and native adapters.
A usable unsigned isolated local preview is built; public EXE release is not
qualified and no backend/public endpoint has been changed.

## User-visible behavior

Install from an ordinary Windows wizard without normal administrator rights.
Production EXE can check for updates in the background or through Settings;
users decide when to install. Notifications wait until a normal WordSuggestor
window is active. Closing follows existing save/cancel prompts. Preview uses a
separate product/data identity and never checks the public feed. Settings and
user words survive upgrade and uninstall. Existing MSIX remains available.

## Implementation details

package_app.ps1 gains an EXE profile using the same self-contained app/Core/DA
staging. Hash-pinned signed Inno Setup is provisioned per-user; a stable product
ID and four-part versions support upgrades. Lower versions are rejected; an
active-app mutex prevents forced replacement. Production setup/app/uninstaller
require signing. Bounded direct-install.json selects distribution; bundled Core
resolution and preview-only qualification use explicit identity/data guards.

WindowsExeUpdateService accepts only a bounded ten-field fixed-host HTTPS feed,
rejects stale offers/duplicates/wrong targets and downloads a length/hash-checked
installer, including the EXE's embedded numeric file version. Windows Authenticode
and exact publisher are checked on download and
again under a no-write/no-delete handle at launch. Timeouts, cancellation and
verification process failure fail closed. Existing one-use update tickets and
save/cancel handoff are reused. ExeUpdateMonitor offers only from active normal
WordSuggestor windows. Windows/macOS shared language logic is untouched.

The existing backend O-052 contract remains MSIX-only. EXE packaging emits a
clearly non-public release-manifest.draft.json; the current SSH uploader and
backend validators are not weakened. A precise backend-owner handoff requests a
compatible EXE/catalog/feed extension before uploader alignment/publication.
The abandoned HTTPS diagnostic prototype is archived only in ignored artifacts;
no TLS root was issued/trusted and no live HTTPS test service was created.

## Files changed

- `/dependencies.lock.json`
- `/docs/Architecture.md`
- `/docs/changes/2026-11-15_wsa_rt_032_exe_beta_distribution.md`
- `/docs/ExeBetaRelease.md`
- `/docs/FinalAcceptance.md`
- `/docs/handoffs/WSA-RT-032_exe_beta_backend_agent_prompt.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/Release.md`
- `/docs/sprints/WSA-RT-032_windows_exe_beta_distribution.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/docs/UiParityPlan.md`
- `/packaging/WordSuggestor.iss`
- `/README.md`
- `/scripts/bootstrap_installer.ps1`
- `/scripts/exe_installer.ps1`
- `/scripts/package_app.ps1`
- `/scripts/test_exe_installer.ps1`
- `/scripts/test_release_scripts.ps1`
- `/scripts/test_settings_app.ps1`
- `/src/WordSuggestorWindows.App/App.xaml.cs`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/Services/DirectInstallation.cs`
- `/src/WordSuggestorWindows.App/Services/ExeUpdateMonitor.cs`
- `/src/WordSuggestorWindows.App/Services/ReleaseQualificationProbe.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.Updates.cs`
- `/src/WordSuggestorWindows.App/Services/SingleInstance.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsDataPaths.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsExeUpdateService.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateCoordinator.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateService.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `/tests/WordSuggestorWindows.Smoke/ExeUpdateSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`

## Feature flags / settings impact

Optional checkExeUpdatesAutomatically (default true) in existing schema 9;
merge/unknown-field preservation retained. Direct production and unique preview
identities; preview updates always disabled. InstallerFormat remains Msix by
default; explicit Exe selects the new route. Production G05 write gate stays
false/read-only; investigation parked by user. No macOS/Core/backend or shared
Docs change. New WSA-RT-032 ID avoids existing macOS sprint identifiers.

## Logging / telemetry impact

No new usage telemetry, document/keystroke logging or credentials. Update traffic
contains only public feed/artifact requests; redirects/cookies disabled. Failure
messages use bounded categories, not native verifier output. Compiler/installer
logs and synthetic qualification records stay in ignored .artifacts. Downloaded
EXEs are user-local. Existing diagnostic privacy GUI checks still pass.

## Validation performed

- Final updater build: zero warnings/errors; 38 checks PASS. Synthetic network
  and launch tests plus actual native trust acceptance of the pinned vendor
  compiler installer, wrong-publisher rejection and unsigned rejection.
- Native standard-user EXE qualification: six PASS. Clean install, real bundled
  Core/DA/user word, N-to-N+1, settings retention, lower-version rejection and
  uninstall with user data retained. Preview ae63f10c4972, versions 0.2.0.1/2;
  record .artifacts/exe-tests/ece50576a30347c4b8164fd312f9a5a7/qualification.json.
- Existing app 105 PASS; existing MSIX update-state 38 PASS; release scripts
  203 PASS; Settings layout/accessibility 1201 PASS (actual DPI 96, simulated
  raster scales only).
- Self-contained EXE payload copied outside source workspace, SDK paths removed:
  55 GUI PASS. Real Notepad, keyboard/mouse/Unicode insertion, clipboard/focus,
  editor/tabs/draft restart retention and diagnostic privacy. One monitor, DPI 96.
- Production-only OCR template branch compiles; test artifact never installed.
- PE version verification is tested separately from textual ProductVersion.
  The pinned vendor installer omits numeric FileVersion (0.0.0.0) despite its
  textual 7.1.1 version; fixtures use observed numeric fields rather than assume
  the two are identical. WordSuggestor sets its numeric version explicitly.
- A transient WPF intermediate-file lock interrupted one build; sequential retry
  passed. No forced process stop, expectation relaxation or destructive git use.
- Full solution fresh-output build: zero warnings/errors; not a new clone or
  network bootstrap test. Final diff/links/secret/artifact checks recorded in
  the sprint/runbook before commit. Native product signing/update is not inferred
  from local preview or synthetic fixture results.

## Known limitations / follow-up

No approved product signing identity or backend EXE contract/feed yet. Production
EXE publisher is unassigned; preview is unsigned, may show Windows reputation
warnings and is not a public release. Real signed HTTPS notification-to-install,
production OCR protocol registration, physical multi-DPI/monitor and spoken
screen-reader testing remain unqualified. Windows Hello remains user-deferred.
G05 is parked, not solved. Existing MSIX receipts retain their measured scope.

Next: install the local preview and compare editor/toolbar/eight Settings pages
with macOS ba18b45 and the supplied screenshots. The backend agent can meanwhile
use docs/handoffs/WSA-RT-032_exe_beta_backend_agent_prompt.md to let beta users
download the EXE and let the app discover newer releases. Then align the Windows
uploader to that authoritative extension and qualify the approved signer and
real signed N-to-N+1 cycle before publication. No public link/upload is enabled
by this commit.

## Consolidation notes for WordSuggestorDocs

Summarize the user's EXE-first decision, parked G05, local qualification scope
and separate production signing/backend gates. Link this repo's ExeBetaRelease,
WSA-RT-032 and backend handoff; keep Windows implementation detail local. Preserve
macOS/Sparkle contracts and prior qualification records; do not copy Windows
implementation notes into macOS docs or reclassify deferred tests as PASS.

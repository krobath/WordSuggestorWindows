# WSA-UX-029 slice 7 — Windows account and Hello candidate

## Summary

Implement Profile/account controls and protected session recovery with the existing
backend contracts. Retain WPF, Core/pack logic and all unrelated working-tree work.
Native Hello positive qualification is explicitly open on this host.

## User-visible behavior

Login/verification-first registration, profile drafts, email/password actions,
entitlement display, separate current/all-device logout and clear connection/lock
states replace the unavailable Profile placeholder. Local suggestions work offline.
Hello protection changes only after the actual protected roundtrip completes.

## Implementation details

Use backend `2f67ddc6` and macOS `ba18b45`; all backend APIs use Jackson snake_case.
Add a bounded HTTPS adapter, cancellable single-flight session coordinator,
Credential Manager envelope/revocation queue, stable nonsecret device identity and
native guarded signing/KDF/AES-GCM adapter. Refresh persists idempotency before
requests and verifies origin/session/user/expiry. Versioned personalization writes
report partial success/conflict. No Settings schema/Core/backend changes.

## Files changed

- `/README.md`
- `/docs/AccountAndHello.md`
- `/docs/Architecture.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/UiParityPlan.md`
- `/docs/changes/2026-11-11_wsa_ux_029_slice7_account_hello.md`
- `/docs/qualification/WSA-UX-029_slice7.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_settings_app.ps1`
- `/src/WordSuggestorWindows.App/Account/WindowsHelloWorker.ps1`
- `/src/WordSuggestorWindows.App/Controls/AccountSettingsView.xaml`
- `/src/WordSuggestorWindows.App/Controls/AccountSettingsView.xaml.cs`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/Models/AccountContracts.cs`
- `/src/WordSuggestorWindows.App/Services/AccountBackendClient.cs`
- `/src/WordSuggestorWindows.App/Services/AccountSessionCoordinator.cs`
- `/src/WordSuggestorWindows.App/Services/AccountSessionVault.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsAccountCredentials.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsHelloSessionKey.cs`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.Account.cs`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `/src/WordSuggestorWindows.App/WordSuggestorWindows.App.csproj`
- `/tests/WordSuggestorWindows.Smoke/AccountHelloNativeSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/AccountSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`

## Feature flags / settings impact

Existing backend flags gate verification-first registration and email change.
Hello policy is secure-credential metadata; no false Settings bool or schema bump.
Automatic/Hello/login-at-launch meanings remain separate. No payment or ranking
change, account auto-creation, backend learning or production publishing.

## Logging / telemetry impact

No passwords/access tokens in persisted settings or diagnostics; protected recovery
is in Credential Manager, with no file fallback. Bounded status codes replace raw
backend/error text. Native worker key material stays in pipes/process memory.
Synthetic test logs print outcomes and capability metadata only.

## Validation performed

See [qualification/native/manual matrix](../qualification/WSA-UX-029_slice7.md).
Fresh exact-source archives passed all seven software gates: build 0 warnings/
errors; Core 335 plus 6 DA fixtures; app 67; Account 55, Analysis 39, Suggestions
48, Speech 102, adapter 58, State 63, UI 58, Speech UI 24; GUI 31, media 3 and
qualification safety 35. The separately executed HelloNative gate is BLOCKED,
exit 3, because the native key service is unavailable. No PIN/biometric or live
backend-account acceptance is claimed. Synthetic credentials were cleaned up.
Final source-tree/fingerprint and manual matrix are in the qualification document.
Only docs changed after that run. Diff review covers whitespace, secrets,
machine-specific paths, generated binaries, local links and unrelated changes.

## Known limitations / follow-up

Positive native PIN/biometric, cancel/lockout/hardware, packaged/unpackaged Hello,
dedicated live-backend acceptance and human Narrator/contrast/DPI remain open.
Account HTTP tests use synthetic existing-contract responses; they do not attest
a deployed backend account. Provider-unavailable cleanup may leave orphan keys
without tokens. Earlier tabs/restart/release work stays outside this commit.
Next engineering slice builds actual App Installer update controls while retaining
the open native Hello/live-account gates; no production release is authorized.

## Consolidation notes for `WordSuggestorDocs`

Later docs-only consolidation should link the Windows account/wire/storage/Hello
contract and its explicit positive-platform qualification gap. Keep implementation
details and live status Windows-local; backend/macOS/Core/shared repos unchanged.

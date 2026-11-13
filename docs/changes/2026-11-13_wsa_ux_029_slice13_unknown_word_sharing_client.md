# WSA-UX-029 slice 13: account-bound unknown-word sharing client

## Summary

Implement the Windows handoff client using the existing Core wire format, durable
idempotent queue and existing secure account transport. The user assigned the
missing backend receiver to the macOS agent, so A07 remains unavailable/off.

## User-visible behavior

Advanced explains the backend prerequisite accurately. No real sharing, public
upload or hidden consent is enabled. Future grant is independent, default-No and
bound to the signed-in profile; revocation clears pending sharing, not local words.
The backend agent prompt is ready to copy from the Windows-owned handoff document.

## Implementation details

Core-owned prepareUnknownWordHandoff exports learned dictionary metadata only.
Windows validates closed schemas, binds one aggregate queue packet to account,
origin and source SHA256, persists its body/key across restart/update and reuses
AccountBackendClient's HTTPS/bearer/idempotency path. A source-reviewed deployment
gate and exact endpoint remain unset, plus a future backend client/config flag.
No user setting/environment variable bypass. Shared library/macOS/backend unchanged.

Atomic file/lease handling preserves corrupt/future main and backup. Source changes
supersede old pending words; acknowledgement and seven-day processed expiry strip
lexical queue copies. Offline/408/429/5xx receive bounded backoff/five attempts;
other errors stop retry. Logout/lock/account actions cancel the sharing request;
current consent/profile are rechecked. A qualified-only 30-second timer coalesces
changes; there is no timer/network activity in the current unqualified production
path. See /docs/UnknownWordSharing.md for precise bounds and failure behavior.

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
- `/docs/UnknownWordSharing.md`
- `/docs/changes/2026-11-13_wsa_ux_029_slice13_unknown_word_sharing_client.md`
- `/docs/handoffs/WSA-UX-029_slice13_backend_agent_prompt.md`
- `/docs/qualification/WSA-UX-029_slice13.md`
- `/docs/qualification/settings-parity-status.v1.json`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_settings_app.ps1`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/MainWindow.UnknownWords.cs`
- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/Models/CoreSettingsContract.cs`
- `/src/WordSuggestorWindows.App/Models/OperationalDiagnostics.cs`
- `/src/WordSuggestorWindows.App/Models/SettingsSchema.cs`
- `/src/WordSuggestorWindows.App/Services/AccountSessionCoordinator.cs`
- `/src/WordSuggestorWindows.App/Services/AccountUnknownWordSharingSender.cs`
- `/src/WordSuggestorWindows.App/Services/LearnedWordStore.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.Advanced.cs`
- `/src/WordSuggestorWindows.App/Services/UnknownWordHandoffPacket.cs`
- `/src/WordSuggestorWindows.App/Services/UnknownWordSharingClient.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsDiagnostics.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliUnknownWords.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/UnknownWordSharingSmoke.cs`

## Feature flags / settings impact

Schema 9 adds explicit sharing preference, consent version and consented account
UserId; older profiles default off. Master lookup revocation also clears sharing
permission. UnknownWordSharingDeployment.Qualified=false and EndpointPath=null.
A07 stays unavailable in the 72-control matrix until real receiver qualification.
Core is pinned to 0278cbc515580f6f9c9e18020e5ed753d9fc7b11.

## Logging / telemetry impact

No raw words, pending/failed surface buffers, documents, credentials, body or raw
backend response in diagnostics. Body is explicit learned metadata in IPC/local
queue only after independent consent. Credentials remain in the existing vault/
account session; no new token config or secret file. No public backend request.

## Validation performed

Build PASS zero warnings/errors; Core six DA fixtures / 675 executable checks;
complete 15-suite Settings All; final 30-check queue/transport suite; 98 app checks,
account state, 24 native automatic typing checks and 41 control/safety checks PASS.
Final targeted suite covers source-change/expiry changes after the initial All.
See /docs/qualification/WSA-UX-029_slice13.md and ignored logs for exact results.

## Known limitations / follow-up

Backend receiver, exact endpoint/capability deployment, real authenticated upload
and server idempotency/retention are NOT qualified. The macOS agent owns those
changes. Keep the source gate false until deployment review and test-account
roundtrip. macOS runtime, human accessibility, Hello and trusted MSIX/update gates
remain separately open. No push, production release or trust change performed.

Next complete installer/update qualification in slice 14, then the final source/
manual/hardware acceptance record in slice 15. Supply the backend prompt to the
macOS agent; no Windows activation follows from mock success alone.

## Consolidation notes for `WordSuggestorDocs`

Record Windows client delivery, schema 9 and user-assigned backend ownership, while
A07 remains unavailable. Shared format is reused without macOS/library conflicts;
keep implementation runbooks local to Windows/Core.

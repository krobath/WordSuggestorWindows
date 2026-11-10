# WSA-UX-029 slice 3 — Core Settings adapter

## Summary

Add negotiated, typed Windows access to existing Core Settings functionality.
Keep WPF, CLI lifecycle, pack/data locations and ordinary v1 suggestions.
Pin the qualified portable Core implementation in the existing lockfile.

## User-visible behavior

No new Settings controls are enabled yet. Later slices can use real source
preferences, explicit domains, morphology and UTF-16 diagnostics; unavailable
pack features/learning remain explicit instead of becoming ineffective controls.
Existing Settings, editor and Notepad behavior is regression-tested separately.

## Implementation details

`ICoreSettingsAdapter` is implemented by the existing provider through a partial
class/shared process exchange. A v1 probe negotiates v2; only explicit old-protocol
rejection downgrades. Typed DTOs preserve scope, separate display/insert forms,
opaque reading IDs, original UTF-16 ranges and grouped edits. Payload/identity
validation rejects corruption, split surrogates and incorrect responses without
content-bearing errors. Future UI callers must recheck current document/generation/
language at delivery. The current normal suggestion path stays v1.

Core revision `c87f68aa2c145220817c0264279854d5b9b32078` wraps shared APIs with
no engine/normalization/pack changes. Disabled memory-only v2 statistics add no
implicit learning writes. Seed catalogs/optional-table fixtures are qualification
inputs, not bundled production features. Full contract: [BridgeContract](../BridgeContract.md).

## Files changed

Paths are relative to the Windows repository root. Shared files are selectively
staged; earlier editor/release changes are preserved outside this commit.

- `/src/WordSuggestorWindows.App/Models/CoreSettingsContract.cs`
- `/src/WordSuggestorWindows.App/Services/ICoreSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs` (transport extraction only)
- `/tests/WordSuggestorWindows.Smoke/CoreSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs` (new gate/child dispatch only)
- `/scripts/test_core_cli.ps1`
- `/scripts/test_settings_app.ps1`
- `/dependencies.lock.json`
- `/README.md`
- `/docs/README.md`
- `/docs/Architecture.md`
- `/docs/BridgeContract.md`
- `/docs/Plan.md`
- `/docs/ParityMatrix.md`
- `/docs/UiParityPlan.md`
- `/docs/ManualSmoke.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/docs/qualification/WSA-UX-029_slice3.md`
- `/docs/changes/2026-11-10_wsa_ux_029_slice3_core_settings_adapter.md`

## Feature flags / settings impact

Request-scoped advanced options only; existing schema 2/data authority unchanged.
No new consent/account/backend protocol, feature default or release channel.
choicePersistence/adaptiveLearning are explicitly unavailable. Later slices own
UI wiring and local learning lifecycle. Positive Hello/OS-update gates stay open.

## Logging / telemetry impact

No new telemetry or raw text/credentials in logs. Structured codes, bounded pipes
and sanitized exceptions only. Synthetic tests/artifacts stay ignored. Returned
lexical/diagnostic payloads are application user content and must not be logged.

## Validation performed

[Slice 3 qualification](../qualification/WSA-UX-029_slice3.md) records commands,
exact-source regression, 169 portable Core checks, 58 adapter checks and the manual
matrix. Fresh exported-source build passes with zero warnings/errors; app 67,
Settings state/UI 63/58, app/Notepad GUI 30, media 3 and qualification safety 35
checks pass. Existing full Core Swift tests fail compilation in unchanged Windows
test/tool files using CFAbsoluteTimeGetCurrent; a full-suite PASS is not claimed.
macOS, human accessibility, hardware and signed-release gates remain open.

## Known limitations / follow-up

Legacy DA has no family/ngram/phonetic data. A successful transport is not completed
UI parity or persisted learning. Cross-platform fixture execution is still needed.
Next slice 4: real speech speed/strategy/highlights; source/domain/analysis and local
choices follow in slices 5–6. Production signing/publication is outside this slice.

## Consolidation notes for `WordSuggestorDocs`

A later docs-only pass may link the Core contract and Windows qualification.
Keep Windows runtime evidence separate from Mac UI/reference and backend docs.
Do not promote seed domains, claim learning or mark the whole Settings sprint done.

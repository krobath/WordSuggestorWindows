# WSA-UX-029 slice 10: completed-word assessment adapter

Date: 2026-11-12. Implementation boundary: read-only Core preflight.

## Summary

Implement the next bounded dependency after slice 9: an explicit, typed Windows
adapter for Core's existing unknown-word observer decision. Core is pinned to
4c3cab1a4717699186461dc34578ff48bb7839b2. Keep WPF, native controls and the current
process/pack boundary. No C# linguistic rules or macOS source changes.

## User-visible behavior

Normal Settings/editor behavior is unchanged. A06/A07 are still disabled/off:
assessment is not online lookup, local learning or sharing. Developers and future
verified-commit callers can explicitly assess one token through the adapter;
`scripts/test_settings_app.ps1 -Mode CommittedWord` exercises that real boundary.
The new gate is part of All. No new user action transmits or stores unknown words.

## Implementation details

Require both capability and operation before sending a token. Use the existing
versioned v2 stdin/stdout transport and document GUID/generation/language scope.
Input is bounded to 128 UTF-16 units; envelope safety is checked locally while
Core owns tokenization, normalization and observer eligibility. Reply schema 1
has only candidate/not_candidate. Reject extra/duplicate/missing fields, unrelated
payloads, invalid schema/disposition and request/scope mismatches. Do not infer
unknown words from empty suggestions or treat numeric observer signals as valid
dictionary words.

Core opens the base pack read-only and reads bounded explicit v2 user words into
a memory-only overlay. It cannot migrate an old file or use global legacy
fallback. Missing snapshots create no directories; future/corrupt snapshots are
protected and fail closed. There is no observer capture, queue, provider or upload.
Repetition is read-only/idempotent; existing deadlines, queued/in-flight
cancellation, bounded output and child reaping apply.

See [the contract](../CommittedWordAssessment.md) and
[slice plan](../sprints/WSA-UX-029_slice10_committed_word_assessment.md).

## Files changed

- /README.md
- /dependencies.lock.json
- /docs/Architecture.md
- /docs/CommittedWordAssessment.md
- /docs/Diagnostics.md
- /docs/ManualSmoke.md
- /docs/ParityMatrix.md
- /docs/Plan.md
- /docs/README.md
- /docs/UiParityPlan.md
- /docs/changes/2026-11-12_wsa_ux_029_slice10_committed_word_assessment.md
- /docs/qualification/WSA-UX-029_slice10.md
- /docs/qualification/settings-parity-status.v1.json
- /docs/sprints/WSA-UX-029_slice10_committed_word_assessment.md
- /docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md
- /scripts/test_settings_app.ps1
- /src/WordSuggestorWindows.App/Models/CoreSettingsContract.cs
- /src/WordSuggestorWindows.App/Models/OperationalDiagnostics.cs
- /src/WordSuggestorWindows.App/Services/ICoreSettingsAdapter.cs
- /src/WordSuggestorWindows.App/Services/WindowsDiagnostics.cs
- /src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSettingsAdapter.cs
- /tests/WordSuggestorWindows.Smoke/CommittedWordAssessmentSmoke.cs
- /tests/WordSuggestorWindows.Smoke/Program.cs

## Feature flags / settings impact

No new user setting, default, consent or migration; settings schema 6, session
schema 1 and all 72 control statuses remain. Only Core lock/status revision is
advanced. The smaller CommittedWordAssessment capability never enables A06/A07.
Hello remains user-deferred and the native update-policy editor stays gated.

## Logging / telemetry impact

No word, normalized text, document, path or raw process stderr enters diagnostics.
The existing opt-in operational summary gains an appended fixed operation enum
with zero result counts. Raw debug stays off; no network/telemetry added.

## Validation performed

Targeted Core executable gate: PASS 505 checks plus six real batch/JSON Danish
fixtures. Targeted new adapter gate: PASS 79 checks before final source-only
integration. Earlier compiled Windows adapter: PASS 58 unchanged checks against
the new CLI, proving the additional capability/operation preserves old clients.
Full measured source/desktop matrix and final counts are recorded in
[slice-10 qualification](../qualification/WSA-UX-029_slice10.md).

An initial new test incorrectly expected an NFD term to behave differently from
the existing exact Core path and omitted the variants row. The fixture now tests
agreement with that independent path, without changing Core normalization. Numeric
tokens retain the actual Core observer behavior. Another new assertion was placed
after an existing suggestions call created a user-word directory; it was moved
before that writing baseline. The mutable baseline is then seeded with an owned
empty v2 overlay so it cannot consult global legacy data; all 79 checks were
repeated after this final test-only amendment. Timeout/cancellation markers are fresh and actual
child exit is checked. Existing tests were not weakened or skipped.

## Known limitations / follow-up

This slice is a foundation, not complete unknown-word learning parity. No normal
typing flow invokes assessment; there is no physical completion receipt yet.
Next supply a reviewed Core-owned provider/store, explicit reversible lookup
consent and verified edit events. Users could then look up and learn genuinely
unknown words locally; A06 remains unavailable until that works. A07 sharing needs
its own reviewed consent/handoff, not implicit L02 consent. Real Hello, trusted
N-to-N+1, human accessibility/mixed-DPI and official beta gates remain open.

## Consolidation notes for `WordSuggestorDocs`

Windows implementation docs stay here; Core documents the portable transport in
its own repo. No shared Docs, backend, macOS or language-pack edits. Compare Core
56aba63..4c3cab1: Sources, public library API, Package.swift, Tests and pack data
are unchanged. macOS ba18b45 consumes the unchanged library product, not the CLI.
No actual macOS build is claimed on Windows. Both repos receive one scoped commit;
no push/publication is performed.

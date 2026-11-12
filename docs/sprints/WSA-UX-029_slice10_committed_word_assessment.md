# WSA-UX-029 slice 10 — committed-word assessment

Parent: WSA-UX-029_windows_settings_sidebar_and_functional_parity.
Date: 2026-11-12. Scope: read-only portable assessment + Windows adapter.

## Outcome and boundaries

Implement the first code boundary needed after slice 9: Core decides whether one
completed token emits its existing unknown-word observer signal. Deliver the
typed adapter, strict capability/payload/scoped identity checks, read-only overlay
snapshot, cancellation/timeout and compatibility tests. No automatic observation,
local-learning store, online provider, sharing or A06/A07 enablement in this slice.
This is a bounded follow-up to the original 0–9 implementation sequence.

## Acceptance

- Existing WPF/Core architecture retained; no C# language rules.
- Additive CLI capability/operation, old clients and ordinary ranking unchanged.
- Shared Core library/Package.swift/packs and macOS source unchanged.
- Explicit one-token request only, result has fixed disposition/schema and scope.
- Future/corrupt/legacy snapshots are protected without migration or global fallback.
- No writing, network, hidden consent or word content in diagnostics.
- Real Core fixtures and adversarial Windows transport/lifecycle tests pass.
- Local documentation, pinned dependency, parity matrix revision and commit IDs
  reflect the actual source; release/native/manual gates remain explicit.

Implementation and validation status: see [qualification](../qualification/WSA-UX-029_slice10.md)
and [contract](../CommittedWordAssessment.md). The 72-control matrix retains A06/A07
unavailable; this foundation does not close full Settings parity.

## Next product work

Build a Core-owned provider/store and explicit lookup consent with verified
committed-token events. Users will then be able to look up and learn unknown words
locally. Sharing stays separately gated until its consent/handoff is reviewed;
native Hello stays user-deferred and beta signing/update acceptance remains open.

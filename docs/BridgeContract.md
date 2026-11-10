# Core CLI contracts — v1 compatibility and negotiated v2

## Current live Settings boundary — 2026-11-11, slice 5

Pinned Core is `7491a0b748ac87d5286b50f4c0c93d1b90c6a84d`. Live suggestions now
use negotiated v2 options/plans when available, retaining v1 fallback for old
providers. Successful metadata-keyed capabilities are cached; failures are not.
Core owns `spacingMode`, suffix-aware `commitPlan`, `smartFollowup` and English
`livePeriod`; Windows validates and applies only verified scoped transactions.
Source switches are sent to Core rather than applied as UI post-filters.
The [Windows host contract](Suggestions.md) complements Core-owned
`docs/140_suggest_cli_settings_bridge_contract.md`. The older slice-3 description
below records the transport-only phase and does not describe current live wiring.

## Settings extension — 2026-11-10, WSA-UX-029 slice 3

Core revision: `c87f68aa2c145220817c0264279854d5b9b32078`. The authoritative
portable contract lives in the Core repository at
`docs/140_suggest_cli_settings_bridge_contract.md`; its executable test is
`Tools/SuggestCLI/test_settings_protocol.py`. Windows owns the typed adapter and
the qualification below. Existing v1 requests/batch results remain supported.

`ICoreSettingsAdapter` uses the existing provider/process gate. A v1 `capabilities`
request discovers schema-1 pack capabilities; only explicit `invalid_request`
from an old CLI permits a v1-only result. Transport/JSON/timeouts never turn into
successful legacy negotiation. Ordinary toolbar/editor suggestions still use v1;
there is no per-keystroke negotiation or latency claim in this slice. Advanced
queries negotiate each time, capture the selected pack/language once and use v2.

V2 envelopes require version, requestId, operation, language, packPath, text,
maxResults (1–40), and context `{documentId: nonzero UUID, generation: Int64 >= 0}`.
Optional fields are operation-specific options, userWordsPath, catalogPath/domainId.
Unknown/inapplicable options fail closed. Stdin is at most 128 KiB, text at most
16,000 UTF-16 units; Windows rejects oversized analysis input instead of silently
truncating ranges. Its shared transport limits stdout to 2 MiB/stderr to 256 KiB,
uses direct executable launch, drains both pipes, kills/reaps failed children and
applies the existing timeout/cancellation policy. Stderr/raw exceptions are private.

| Typed API | Core authority | Missing-data behavior |
|---|---|---|
| SuggestWithOptionsAsync | Profile source switches and optional PackNgramReranker | Missing ngram data explicitly rejects contextBased=true; no C# after-filter |
| GetDomainsAsync / SuggestDomainAsync | Explicit filesystem catalog / specialized suggestions | No catalog means empty; invalid catalog warns; unknown domain rejects; display and insert forms remain separate |
| GetWordInfoAsync | analyses, ProductWordClassMapper, lemmaFamilyHits | Existing Core fallback readings; missing term_variants gives empty families and warning |
| AnalyzeAsync | ContextDiagnosticsEngine; semantic/punctuation toggles, both comma styles/modes | Only da-DK is qualified; disabled sources produce no findings |

All v2 responses must match version/requestId/documentId/generation/language before
payloads are returned. `CoreQueryResult.IsCurrent` is a second delivery check that
future UI callers must compare with their current document/generation/language on
the dispatcher. It does not automatically discard a DTO retained by arbitrary
caller code. Counts, required strings, finite scores, diagnostic/edit bounds and
surrogate boundaries are checked; malformed output never becomes an empty success.
Original UTF-16 offsets, grouped corrections, requirements and separate insertText
cross intact. Reading IDs are opaque `analysis_v1:` values, scoped to Core/pack,
not automatically persisted learning keys. No diagnostic edits are auto-applied.

Capabilities are selected-pack dependent, not a guarantee of suggestions. The
bundled legacy DA lacks family/ngram/phonetic evidence. The tested ai_ml_llm
catalog is a tracked Core **seed**, explicitly selected by tests; Windows neither
bundles it nor silently discovers/promotes it. Modern pack validation below and
the existing data locations remain unchanged. Local compatible user words can
participate in v2 suggest; a corrupt/future overlay is preserved with a warning.

Choice persistence/adaptive learning are explicitly false. V2 uses disabled,
memory-only statistics in both ranking lanes and adds no storage/consent/backend
operation. Feature UI wiring remains slices 5–6; speech is next in slice 4.
See [slice 3 qualification](qualification/WSA-UX-029_slice3.md) for tests and open
Mac/full-Swift-suite/hardware gates.

## Existing v1 transport and pack boundary

Updated 2026-11-08, WSA-RT-016. The portable implementation is
`WordSuggestorCore/Tools/SuggestCLI/StdioProtocol.swift`; the Windows client is
`WordSuggestorCoreCliSuggestionProvider`. Ranking, matching, normalization and
user-word merging remain in Core. The existing batch CLI is still supported.

## Transport

Launch the prebuilt executable directly with `--stdio-json-v1`. Send one UTF-8 JSON
request on stdin, close stdin, and read one JSON response on stdout. No shell,
document arguments or request files. Stderr is drained and never copied to
diagnostics or UI. One child per request; cancellation kills and reaps its process
tree. A provider serializes requests and applies an eight-second deadline.
Measured Windows debug calls were approximately 39–326 ms in the observed runs;
these are smoke observations, not a latency percentile benchmark.

```json
{
  "version": 1,
  "requestId": "unique-request-id",
  "operation": "suggest",
  "language": "da-DK",
  "packPath": "absolute path supplied by the pack adapter",
  "textBeforeCaret": "Jeg vil gerne skri",
  "maxSuggestions": 40,
  "userWordsPath": "absolute user-data path supplied by the host"
}
```

`operation` is `suggest` or `addUserWord`. For the latter, `textBeforeCaret` holds
the single explicitly entered word. Core normalizes it and persists its existing
`local_lexicon_overlay_v2` format. Missing optional `userWordsPath` gives memory-only
suggestions. Input is bounded to 16,000 UTF-16 units / 128 KiB JSON. Candidate count
is 1–40. There are no network calls or credentials in this contract.

```json
{
  "version": 1,
  "requestId": "unique-request-id",
  "suggestions": [
    {"term":"skrive","score":6.8,"kind":"prefix","type":"word","pos":"verb","gram":"vb.inf.akt"}
  ]
}
```

The example score is illustrative. `pos`/`gram` are optional. `errorCode` is an
optional machine-readable failure; `warningCode=user_words_unavailable` permits
base-pack suggestions while preserving an unreadable/newer user-word file. Unknown
user-word schemas cannot be overwritten by `addUserWord`. Responses never echo
document input. Windows verifies protocol version, request identity, candidate
shape and finite scores, bounds stdout/stderr and discards stale/cancelled results
again inside the UI dispatcher. Error UI uses exception category, never raw stderr.

## Packs

Installed packs: `%LOCALAPPDATA%\WordSuggestor\Packs`; old roaming
`%APPDATA%\WordSuggestor\Packs` stays readable without moving files. Bundled packs
live in read-only `Packs` beside the executable. Development additionally searches
the adjacent Core `Ressources` directory. Valid installed packs take precedence;
semantic metadata versions are ordered numerically. Invalid candidates are skipped.
New packs currently require restart for discovery.

`LanguagePackValidator` opens SQLite read-only through Windows' `winsqlite3`, checks
integrity, required terms columns, schema, language and metadata/version consistency.
Modern packs require schemas 8–14 and the parity-profile tables. Data version is not
an engine ABI version. No Windows-specific pack migration is performed.

The supplied `da_lexicon.sqlite` is a legacy schema-0 pack without `pack_meta`.
Only that explicit Danish legacy profile is accepted without metadata. It must not
be presented as a modern first-class pack. The empty English resource is rejected;
English/German routing remains available for compatible packs.

## Reproduction and parity

`scripts/test_core_cli.ps1` builds Core and compares six real Danish fixtures
(`skri`, `læ`, `ø`, `å`, `smør`, `blå`) between batch and JSON routes, including
ordered terms, kinds and scores. `scripts/test_app.ps1` tests the real C# adapter,
failure hosts, timeout/cancellation, stale responses and persistent Core user words.
This proves Windows transport parity with Core, not a macOS-machine test. Run the
same fixtures with the same Core commit/pack hash on macOS before cross-platform
quality sign-off. No macOS source was modified.

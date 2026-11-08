# Core CLI contract v1

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

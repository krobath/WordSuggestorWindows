# Windows Advanced diagnostics and privacy contract

WSA-UX-029 slice 9, 2026-11-12. Retain the native WPF shell and Core language logic.
Reference: macOS `ba18b451372dd2e3f125e557c5db45894f4e1ae3`, Advanced A01–A08.
See [qualification and 72-control mapping](qualification/WSA-UX-029_slice9.md).

## Control meaning and current support

| Control | Effective behavior | Default / boundary |
|---|---|---|
| A01 performance | Counts, total and maximum microseconds for suggestion refresh, Core roundtrip, overlay placement and NativeEdit commit | Off; aggregate summaries, no individual keys/text or performance SLA |
| A02 App | Fixed app workflow events such as applied settings, local/external session, completed suggestions and accepted/rejected commits | Off; independent of other diagnostic categories |
| A03 placement | Confirmed caret/static/fallback state and NativeEdit verified/rejected commit route | Off; no screen coordinates, handles, PIDs or window titles |
| A04 panel state | Deduplicated transitions between hidden/local/external suggestion panel | Off; new independent persisted flag |
| A05 Core | Safe transport outcome/timing plus capability-negotiated Core operation summary | Off; raw Core debug logger/stderr is never enabled or forwarded |
| A06 unknown-word lookup | Visible disabled unchecked control with explanation | Unavailable; not manual user-word insertion |
| A07 unknown-word sharing | Visible disabled unchecked control with explanation | Unavailable; no consent or upload is created |
| A08 legacy | Existing separate Windows settings, sharing the same settings authority | No recursive dialog or copied macOS neural/development tooling |

All five active switches persist after successful settings writes. Failed writes
restore effective UI values through the existing controller. Each category is
independent, so panel/Core diagnosis does not require App events. The UI has native
checkboxes, explicit DA/EN accessibility names, help and bounded local-write status.
Mouse, Space and accessibility Toggle use the same persistence path. Write failure
is reported without throwing into typing or falsely claiming a file write.

macOS's initialization of App debug/unknown lookup to true is reference behavior,
not a new Windows default or permission to perform external lookup. Screenshots'
selected values are not migrated as user consent.

## Storage, data and migration

Schema 6 adds `isPanelParityLoggingEnabled` (default false). Existing four debug/
performance preferences retain their explicit true/false values. New profiles
default all five off. Current settings location and existing data ownership remain:
`%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json`. On first migrated write,
the exact old v5 bytes are kept at `.v5.bak`; earlier source-version backups retain
their existing names. Future/corrupt formats stay read-only and untouched. Clone
and three-way merge preserve panel preference, unknown fields and false consent.

Both primary and legacy surfaces use the same successful-write event. A legacy
draft cannot overwrite newer panel settings or drop fields it does not present.
Changing diagnosis does not delete documents, user words, packs, local choices,
account credentials or older diagnostics. Credential Manager storage is unchanged.
No fake effective unknown-word boolean is persisted. Existing unknown extension
fields are retained; they do not authorize new behavior.

New runtime files are fixed under:
`%LOCALAPPDATA%\WordSuggestor\diagnostics\runtime\runtime-v1.jsonl`, plus `.1`/`.2`.
Per-process `WindowsDiagnostics` uses a 128-item channel, nonblocking producers,
at most 128 admitted records per monotonic second and a background writer. Queue/
rate overflow increments a dropped counter; it does not block the UI. Performance
has four enum-keyed aggregates, capped at one million observations per summary,
with per-observation time capped at 60 seconds. Every ten seconds, aggregate
count/total/max is queued without raw samples. Disabled metrics are not collected.

Each file is capped at 64 KiB (three fixed files, about 192 KiB total). Rotation
touches only those fixed names. Owned files older than seven days are removed
lazily when enabled collection writes again; disabled collection performs no I/O,
so old files may remain until collection resumes or the user removes them.
Disabling/reconfiguration invalidates queued epochs and clears aggregates. One
already-started metadata append may finish after a toggle. Existing files are
preserved. Disposal invalidates producers/queue and stops the summary timer; app
shutdown does not synchronously wait for disk. Explicit flush is for tests/tools.

The older operational breadcrumbs used by OCR/TTS/selection are preserved as
separate pre-existing diagnostics; these switches govern the new runtime schema.
They do not retroactively delete/migrate historical files or redefine prior
operational recording. No broader old-log privacy approval is inferred here.

## Closed metadata boundary

`RuntimeDiagnostic` permits schema version, UTC timestamp, enum category/event/
operation/metric, aggregate timing/counts and bounded engine result counts. There
is no arbitrary message/text/path/exception parameter. Producers use fixed enums
instead of sanitizing raw strings. Panel/caret events are deduplicated state
changes, not geometry or text samples. Transport faults use codes for completion,
failure, rejection, timeout or cancellation; raw stderr is drained and discarded.

No document/word/candidate text, keystroke, clipboard, process/window/document/
request ID, file path, backend credential or token belongs in these files. No
telemetry, export, automatic upload, sharing endpoint or credential access is
added. A diagnostic preference is not permission for user-content collection.

The sink is owned by `MainWindowViewModel`; the injected Core adapter shares that
sink and is detached on teardown. No process-global mutable logger is required.
Timed scopes measure existing orchestration without moving ranking, tokenization,
normalization or language decisions into Windows.

## Core operational contract

Pinned Core: `4c3cab1a4717699186461dc34578ff48bb7839b2`. Its existing JSON v2 adds
capability `operationalDiagnostics`. Only when A05 is enabled and capability true
does the host send optional `includeOperationalDiagnostics: true`. The Core-owned
summary contains schema 1, validated existing operation, completed/rejected status,
elapsed milliseconds ≤60000, candidate/diagnostic counts ≤40 and word count ≤512.
It contains no free-form logger messages or identity/content fields. The host
validates exact DTO fields, schema, operation/status and bounds before recording.

Raw `WordSuggestorCoreDebug` and `WORDSUGGESTOR_CORE_DEBUG` remain off in the child.
An older Core without the capability receives no new request field and offers
safe connection metadata only, with an explicit UI explanation. Bad/absent required
summary is a protocol error, never a reason to copy raw response/stderr. The Core
contract is authoritative in its own
`docs/140_suggest_cli_settings_bridge_contract.md`; Windows owns local categories,
retention, UI and platform evidence. No pack format or ranking changes.

## Unknown-word dependency and remaining acceptance

The CLI still advertises unknownWordLookup/unknownWordSharing false. Slice 10
adds only a separately negotiated, read-only completed-token assessment; it has no
committed-word capture, lookup provider, learning store or handoff operation. macOS uses
UnknownWordLearningService plus an external provider; that is distinct from saved
interpretation choices, user words and diagnostic counts. Windows must not infer
unknown words from an empty suggestion list, add words automatically in C#, start
a new HTTP provider, or turn L02 consent into unknown-word sharing.

Before these controls can become effective, Core needs a versioned portable
committed-word/provider boundary, local-store ownership/atomicity, cancellation/
idempotency/offline/rate/error behavior and explicit no-sentence/no-keylog privacy
fixtures. A07 needs its own reversible consent and reviewed handoff; no hidden
upload or production endpoint follows merely from enabling A06. This is concrete
follow-up work, not a claim that all 72 controls or the sprint are completed.

Human Narrator/high contrast/DPI/multi-monitor and trusted installed update tests
still require acceptance. Hello remains deferred by the user, and G05 production
editing stays source-gated. No signed public beta/release is qualified by these
software tests. Next work completes real Windows acceptance and designs the
missing Core lookup lane before users can enable it.

Slice-10 assessment emits only the existing fixed metadata summary when A05 is
enabled, never the assessed token. See [the narrow transport boundary](CommittedWordAssessment.md).
The assessment capability cannot make A06/A07 effective or grant storage/network
permission; their original controls remain unavailable.

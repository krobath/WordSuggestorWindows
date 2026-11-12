# WSA-UX-029 slice 11: explicit unknown-word import

Date: 2026-11-13. Retain WPF/.NET 10 and the shared Swift Core/SQLite pack.
A06 is **partial**: explicit, user-initiated import works; passive observation is
not enabled. A07 sharing is unavailable. No backend or macOS app source changes.

## User workflow and consent

Open Settings > Advanced > Look up unknown word, or the corresponding internal
editor menu action. Enter one Danish word, enable online lookup and approve the
separate default-No explanation. Choose Look up and save. The chosen word goes to
ordregister.dk and possibly ordnet.dk; no surrounding document is sent. Only an
exact match with Core's existing metadata requirements can become a local entry.
There is no automatic typing/clipboard listener, document insertion or upload.
Opting out cancels pending UI work; existing imported words still provide offline
suggestions until the user explicitly clears the separate store and its backup.

Settings schema 7 adds `IsUnknownWordLookupEnabled` (default false) and
`UnknownWordLookupConsentVersion` (required 1). All schemas below 7 migrate with
lookup off, including files containing previously unknown consent-shaped fields.
Exact `.vN.bak` migration copies and unknown settings fields are preserved.
Future/corrupt settings remain read-only; the dialog cannot grant permission.

## Core and Windows boundary

Capability-negotiated JSON v2 adds `lookupUnknownWord` and `mergeLearnedWord`.
Both require a valid document/generation/language scope, one <=128 UTF-16 token
and `lookupConsent: 1`. Capabilities: `unknownWordLookup` for da-DK only,
`explicitUnknownWordImport`, `learnedWordOverlay`; automatic observation and
sharing remain false. Older Core never receives these new operations.

Lookup returns `unknownWord: {schemaVersion:1,status:"resolved",proposal:{...}}`
or a fixed status. Core reuses CORUnknownWordLookupProvider and
UnknownWordLearningService with memory-only persistence and one attempt. Prepare
replays the proposal through the same Core selector, merges a bounded read-only
v2 learned snapshot and returns `{schemaVersion:1,status:"prepared",snapshot:...}`.
Neither phase writes files. Known/short/ineligible/not-found/unavailable/full-store
and missing-consent outcomes never produce a snapshot. No C# normalization,
selection, ranking or pack-format fork is introduced.

Windows `UnknownWordImportService` checks persisted consent and initiating UI scope
after each await. It holds a cross-process FileShare.None lease across prepare and
commit. `LearnedWordStore` writes a uniquely named temporary file, flushes it,
checks cancellation/consent/context and unchanged source fingerprint, then performs
synchronous File.Replace/File.Move on the UI dispatcher. Cancellation, close,
deactivation, input/language change or consent revocation prevents a late commit.
No document target is touched. Invalid replies, duplicate/extra JSON fields and
wrong request/document/generation/language identities are rejected without content
in errors. New diagnostic operations contain only the existing fixed counters.

## Storage and transport

`%LOCALAPPDATA%/WordSuggestor/UnknownWords/learned-v1.json` is a separate file using
Core's existing `local_lexicon_overlay_v2` format (the filename is not schema v1).
It has a `.bak` snapshot and a `.lock` lease file; OS handle closure releases the
lease even after a crash. Core merges entries, Windows commits only returned bytes.
Manual UserWords, language packs and local choice records are untouched. Imported
words participate in v2 suggestions and assessment regardless of current online
consent. Clearing affects only this owned file and backup; incompatible/future
main or backup files are preserved and reported unavailable. No silent recovery
from a backup or schema downgrade. The limit is 1 MiB / 8192 entries / 16 languages.
Existing per-user data/update paths remain unchanged.

The CLI's bounded ephemeral HTTPS transport allows only ordregister.dk/ordnet.dk,
port 443, no credentials, cookies or cache, no redirects, <=256 KiB responses and
3-second request/4-second resource limits. Non-success/non-404 status is an error.
Core's overall lookup deadline is 12 seconds; the Windows process deadline is 15
seconds. Cancellation terminates/reaps the process through the existing bridge.
Explicit clicks are serialized with a one-second dialog throttle; there is no
background retry. A completed external HTTP request cannot be unsent by revoking
consent, but it cannot subsequently persist through the cancelled dialog.

## macOS compatibility

Core adds an optional `requestTransport` initializer parameter defaulting to nil;
existing macOS call sites keep their transport. Two shared provider fixes recognize
the real public `COR-id` key (in addition to old aliases) and encode Unicode URL
path components exactly once. This intentionally makes valid dictionary records
usable on both platforms; language rules, selector and existing API calls remain
shared. Package.swift, pack bytes, engine/ranking and macOS source are unchanged.
Native macOS build/runtime must be checked on macOS; Windows tests do not attest it.

## Tests and remaining work

See [qualification](qualification/WSA-UX-029_slice11.md) and
[slice plan](sprints/WSA-UX-029_slice11_explicit_unknown_word_import.md).
Deterministic CLI QA switches `--lookup-fixture` and `--lookup-response-fixture`
exist only as explicit executable arguments; the app never emits them. The latter
exercises the actual COR parser, metadata selector and Unicode URL construction.
Test children forward directly to Core without a shell. Synthetic imported terms
and settings stay under ignored `.artifacts`, never in personal app data.

Next, connect verified completed-word events to the same consent-controlled
service so users can learn words during writing. This requires dedicated focus,
document-generation and rate-limit tests. Automatic collection and sharing stay
off until that work is reviewed. Hello remains user-deferred; signing, installed
updates, human Narrator and mixed-DPI qualification remain separate open gates.

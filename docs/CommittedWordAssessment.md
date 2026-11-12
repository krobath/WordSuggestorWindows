# Completed-word assessment boundary

WSA-UX-029 slice 10, 2026-11-12. Core revision `4c3cab1a4717699186461dc34578ff48bb7839b2`.
Reference macOS app: ba18b451372dd2e3f125e557c5db45894f4e1ae3.

## Purpose and current behavior

Before adding unknown-word learning, establish whether Core's existing observer
would emit for a single completed token. `ICoreSettingsAdapter.AssessCommittedWordAsync`
now implements that explicit read-only query. No normal editor/external input
flow invokes it yet. A06 lookup and A07 sharing remain visible, disabled and off;
their availability must not follow from this lesser assessment capability.
This slice is the first foundation of the dependency recorded after slice 9,
not full learning parity or a new diagnostic content collector.

## Contract and engine ownership

Negotiate JSON v1 capabilities: both `committedWordAssessment=true` and v2 operation
`assessCommittedWord` must exist. An older Core or partial advertisement fails
before the token is sent. The ordinary v2 request contains one `text`, original
document GUID/generation, language/pack, maxResults and optional explicit existing
Core user-word path. 1–128 UTF-16 units, no whitespace/control characters; Core's
unchanged tokenizer must recognize exactly one whole-buffer token. No options,
sentence context, provider URL, storage destination, consent or raw key event.

Response: `committedWord: {schemaVersion:1, disposition:"candidate"|"not_candidate"}`.
The candidate is an observer signal, not proof of spelling, dictionary validity,
lookup eligibility or consent. Existing Core length/normalization/exact-lookup
rules decide. Numeric tokens can emit under the unchanged observer. NFC/NFD
recognition follows the existing Core exact path; Windows adds no normalization.
Unknown/extra/missing/duplicate response fields, invalid schema/disposition,
unrelated payloads and mismatched request/document/generation/language fail closed.
Only the fixed metadata is returned; no input/normalized word is echoed.

The CLI-only exact source opens SQLite read-only without bundle copying. Existing
user words are read as a <=1 MiB v2 snapshot (<=16 language tables, <=8192 entries)
into a memory-only Core overlay. Missing files create no files/directories.
Corrupt/future/legacy/oversized/inconsistent snapshots reject with
`user_words_unavailable`, preserve bytes and do not fall back to a global legacy
file. This avoids the normal file-backed overlay's migration behavior. Automatic
migration still belongs to its existing user-word flow, outside this query.
The portable contract is authoritative in Core's
`docs/140_suggest_cli_settings_bridge_contract.md`.

## Lifecycle, privacy and compatibility

The existing serialized process gate, bounded UTF-8 stdin/stdout, deadline,
cancellation and child-process reaping apply. Cancellation while waiting starts
no extra word process; cancelling an active request kills only that owned child.
The result carries caller identity for stale/tab/language checks. There is no
transaction receipt: a future host must verify actual completion/focus separately
and discard stale work; this query cannot establish physical user intent.

Assessment has no durable state, so retries are idempotent and cancellation cannot
leave a partial write. There is no learning queue, retry scheduler, provider,
network or consent migration. Existing user settings/schema 6, tabs/session schema
1, user words, packs and secure credentials remain unchanged. No content enters
diagnostics; optional A05 adds only the existing fixed summary with the appended
AssessCommittedWord operation enum and zero result counts. Raw Core debug stays off.

The Core change is confined to Tools/SuggestCLI, executable tests and local docs.
Sources/WordSuggestorCore, Package.swift, Tests and pack data are unchanged from
56aba63. macOS Xcode uses product WordSuggestorCore via its local package; it does
not call this executable. Existing batch/v1/v2 ordering is checked. This establishes
source/API/transport compatibility, not an actual macOS build on Windows.

## Reproduction and remaining work

From WordSuggestorWindows:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode CommittedWord
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode All -SkipBuild
```

The new mode is included in All; HelloNative and UpdateNative remain separate.
See [measured evidence](qualification/WSA-UX-029_slice10.md). No product secrets are
needed; all new content fixtures are synthetic. Ordinary Windows build/run and
release scripts are retained.

Next slice must supply a reviewed Core-owned provider/store, explicit reversible
lookup consent (including any dictionary transmission), verified completion
receipts, cancellation/idempotency and offline behavior before A06 is enabled.
Users would then be able to look up and learn genuinely unknown words locally.
A07 requires a separate reviewed handoff/consent; no implicit backend upload,
ranking change or global learning follows from the new assessment. Hello remains
user-deferred; trusted install/update and human accessibility/DPI remain open.

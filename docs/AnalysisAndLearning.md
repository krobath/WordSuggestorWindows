# Windows text analysis, word choices and local insights

WSA-UX-029 slice 6, 2026-11-11. WPF and the existing Core process bridge are
retained. Core is pinned to `defa19bdc6a3fcb990948caa1982631ced420dbf`.
See [qualification](qualification/WSA-UX-029_slice6.md) for actual test boundaries.

## What the controls do

T01 starts/cancels internal-editor document analysis. T02 paints Core word-class
colors without changing the document, selection, caret or undo. T03/T04 request
Core semantic/punctuation diagnostics. T05 selects grammatical comma with/without
start comma. T06 switches Core `boundary_local_contextual_v1` / `legacy_graph_only`.
Dependent controls disable with the master/punctuation control, while retaining
their saved intention. Non-Danish packs cannot enable Danish diagnostic rules.
Missing Core/packs show a calm unavailable state; no C# suffix/POS guesses return.

Right-click a marked word or punctuation to review actual Core readings and
corrections. Reading titles are localized presentation of Core's class/grammar.
Explicit choices can update the current analysis display; they do not change
engine ranking. A meaning-dependent correction asks for confirmation; a required
grammar obligation is not incorrectly treated as uncertainty. No automatic
correction is introduced. Native cut/copy/paste/select-all remains available.

L01 allows recording explicit interpretation choices locally. It defaults **off**
for profiles without that consent. Turning it off also disables L02. L02 records
separate sharing consent but starts no upload, queue, backend call or training.
L03 opens a detail panel within Learning, with word/interpretation/local status/
time, Back, delete-one and confirmed delete-all. It is not the user-word editor.
L04 confirms resetting local choice storage/sharing preferences and deleting
this store. Documents, user words, packs and error insights are preserved.

E01 retains the existing local writing-insights master. E02 explicitly permits
local sentence fragments and disables when E01 is off; it is independent of L02.
Corrections record only metadata by default. With E02 enabled, Core's exact
bounded example range may be stored in the separate insights content file.
Turning tracking off stops metadata and content writes without deleting history.

## Engine, scope and editing

The negotiated `document` operation supplies original UTF-16 ranges, product word
classes, source-backed ambiguity/readings and existing diagnostic groups/edits.
Core reuses its own tokenizer, morphology, mapper and resolver. Windows owns
presentation, cancellation, exact range validation and native transactions only.

`MainWindowViewModel.Analysis` invalidates results immediately on text/document/
language changes, coalesces work for 350 ms, and checks generation, language and
the exact original text again at dispatcher delivery. Identical text in different
editor activations cannot authorize an old transaction. A selected pack SHA-256
scopes saved reading IDs; no filename/user path is stored in the choice record.
The existing provider still bounds process lifetime/output and reaps cancellation.

`AnalysisRangeAdorner` paints independently of WPF text formatting. It retains
native undo and uses system highlight outlines in high contrast. Right-click
menus suppress the suggestion overlay while open. Explicit mouse/keyboard/menu
actions converge on one once-only path; an owned menu may return focus to its
verified originating editor. Another foreground app, changed document or stale
analysis fails closed. UI Automation's fallback must also verify the active editor.

`DiagnosticEditPlanner` validates UTF-16/surrogate boundaries, bounds, overlap and
duplicate zero-width edits before any change. Group edits apply in descending
offset order within one native BeginChange/EndChange transaction. Caret shifts
are computed structurally, not by language rules. The editor rechecks text/focus
before applying, and a stale/double commit is rejected. External application
analysis/corrections are not enabled; Notepad suggestions remain a separate route.

Editor offsets are bounded to the plain-text buffer; WPF's final paragraph
terminator is excluded even for Select-all, preventing out-of-range input edits.
Native panel placement resolves the HWND before reading bound dimensions and
rejects unusable sizes. The overlay has a 292-DIP minimum/fallback width so its
mouse targets remain inside the actual window. External settings changes request
fresh suggestions; lane hotkeys use the WM_HOTKEY modifier snapshot after key-up.

## Storage, migration and failure handling

Settings schema 5 keeps the historical settings path. The first successful write
retains byte-exact `.v<original>.bak`; schema-4 profiles get `.v4.bak`. Legacy
`Standard` comma style maps to the existing Core default without start comma;
invalid explicit styles fail closed. An invalid contextual mode falls back to
graph-only. False/unknown consent data and unrelated preferences survive. Existing
speech rates are not converted again. Older binaries refuse the future schema.

`%LOCALAPPDATA%\WordSuggestor\learning\word-choices-v1.json` is a versioned,
atomically replaced, bounded local store: at most 5,000 records / 8 MiB. It stores
chosen word, opaque reading ID, Core class/grammar, language, pack hash and UTC
time. Reading IDs may encode lexical metadata; they are not anonymized content.
No full document, neighboring words, raw keys or credentials are recorded there.
Upload/ranking eligibility stays false. No backup containing deleted records is
created by this store. Unsupported/corrupt data is preserved and blocks normal
writes; only explicit confirmed deletion/reset may replace this owned store.

Reset first persists storage/sharing disabled, then clears choices. If the second
step fails, choices remain, capture stays disabled and the UI reports failure;
retry deletion. Reset never resets the interface language or clears other stores.
Ordinary settings continue using three-way merge/atomic save. Delete/reset errors
are shown without raw exceptions or false success.

Insights stay at `%LOCALAPPDATA%\WordSuggestor\insights\error-insights.jsonl`.
Sentence examples are opt-in user content, separate from technical diagnostics
and sharing consent, capped by Core to 512 UTF-16 units. Neither file is uploaded.

## Limits and verification

Analysis accepts at most 16,000 UTF-16 units and reports an explicit partial state
after 512 word spans. Larger documents remain editable with suggestions; analysis
does not silently truncate offsets. Unknown words remain unclassified unless
Core supplies evidence. Existing Core spelling/source behavior is not replaced.

Run `scripts/test_settings_app.ps1 -Mode AnalysisState` for state/privacy/Core
transactions; `-Mode AnalysisUi` tests real WPF mouse/undo/choice/picker behavior.
`-Mode All` includes it and existing settings/suggestion/speech regressions.
`test_core_cli.ps1 -SettingsContract` qualifies the portable contract. Native
automation is not human Narrator/audible/high-DPI/multi-monitor signoff.
Next slice builds the Profile/account surface and Windows Hello using existing
backend/security contracts. This slice enables no account, update or release flow.

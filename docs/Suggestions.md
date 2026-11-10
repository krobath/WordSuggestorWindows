# Windows suggestion preferences and commit boundary

WSA-UX-029 slice 5, 2026-11-11. Implementation candidate; interactive desktop
qualification is still open. See [evidence](qualification/WSA-UX-029_slice5.md).

The WPF shell and existing Core CLI remain. Ranking, source eligibility,
normalization, morphology and punctuation eligibility stay in Swift/Core.
Windows owns scheduling, presentation, verified targets and native editing.
Core is pinned to `7491a0b748ac87d5286b50f4c0c93d1b90c6a84d`.

## Settings and data

Settings schema 4 adds a nested `suggestions` record. The historical
`%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json` path stays; on the first
successful migration write the byte-exact old file is kept in `.v<old>.bak`.
Schema-3 speech rates are not converted again. Invalid/future/null data fails
closed without overwriting it. Unknown fields, false privacy choices, unrelated
preferences, user words and packs survive. Legacy edits use the existing recursive
three-way merge; independent suggestion fields can merge, conflicting changes fail.

F01 retains the global-capture preference; it does not turn off the independent
internal editor. F02 selects fixed 34/40/48-DIP rows: word only, word/source, full
metadata. F03 selects Core insertion mode Smart/Always/Never. F04 selects five
actual scheduling policies: defer 20/35/55/85/110 ms; rapid-typing quiet windows
140/200/260/380/500 ms. Latest requests cancel queued/in-flight work; the existing
process gate bounds child execution. These numbers are scheduling parameters,
not measured end-to-end latency promises.

F05–F07 use unique Ctrl, Ctrl+Shift and Alt digit schemes, defaulting to ordinary,
primary and secondary respectively. Selecting an occupied scheme swaps the other
lane atomically. Exact modifiers exclude AltGr (Ctrl+Alt). Ctrl+Tab remains owned
by the editor. External digit registration is all-or-nothing **per lane**; an
unavailable lane reports a conflict and retains mouse acceptance. Registrations
exist only for active visible results and are released during fading/focus loss.
Ctrl+Left/Right remains ordinary paging; domain lists have separate page buttons.

F08 uses the shared native speech service after 300 ms hover, cancelled on leave,
new results, page/scope/settings changes and hiding. The opaque speech identity
prevents a late hover cancellation from stopping a newer manual utterance.
F09 asks Core for actual analyses/families; family actions enter the same commit
transaction, with current-scope and normal target checks. No inferred C# forms.
F10 asks Core for **English newline period eligibility in the internal editor**,
only after direct composition and a physical line break within 8 seconds.
External auto-period rewriting is deliberately not qualified/enabled.

F11 waits 0–30 seconds, fades over 3 seconds and really hides the panel; 31 means
never. Acceptance is disabled during fading; fresh input restores it. F12–F16
pass context/confusion/phonetic/spelling/synonym preferences to Core. Missing pack
data disables the affected controls with an explanation, retaining saved intent.
The legacy DA fixture has no ngram/family/phonetic data. Pack availability is not
a promise of candidate quality or an implied modern pack upgrade.

F17–F19 use an explicitly installed Core-format catalog at
`%LOCALAPPDATA%\WordSuggestor\Packs\<language-code>\specialized\catalog.json`.
Primary/secondary IDs are retained per writing language; secondary differs from
and depends on a valid primary. Ordinary plus two domain lists have at most 40
results, 10 per page, with independent paging. No catalog means calm disabled
selectors. Missing IDs survive unrelated saves. The Core seed is never bundled,
downloaded or promoted automatically; test copies are explicitly isolated fixtures.
F20 changes FlowDocument page width/scrolling without replacing text, caret or
the document. All existing editor surfaces remain owned by their current code.

## Commit safety and transport

Negotiated v2 suggestions carry Core `insertText` and an optional complete
`commitPlan {text,autoSpace}`. Windows validates the plan structurally and does
not append a second space. Old providers/CLIs retain the existing v1 fallback.
Successful capabilities are cached against language, pack/CLI/catalog path,
length and timestamp; failures are not cached. No new process transport exists.

Mouse, digit and family selection converge on the existing commit transaction.
Local requests check current text generation, prefix and actual editor focus.
External candidates retain the **original full target snapshot**, including
control/window/PID/process start/text/selection. Before the exact native edit,
the adapter re-reads and compares it; focus/control changes and duplicate commits
fail closed. No clipboard fallback, SendInput text injection or C# ranking added.

Smart followup holds an in-memory receipt for 1.5 seconds. Core classifies the
immediate input; the host replaces only the verified auto-space plus that input
after rechecking text/caret/focus/language. The qualified external route remains
Notepad's native edit adapter; Word, Outlook, browsers and Docs are not newly
supported. Unsupported/elevated targets retain the existing safe failure state.

## Verification and limits

Run existing `test_settings_app.ps1 -Mode SuggestionState` for state, real Core
commit and scheduling checks without interactive input. Run `-Mode Suggestions`
on an unlocked desktop for the native keyboard/mouse/lane/Notepad fixture; `All`
includes that gate. `test_core_cli.ps1 -SettingsContract` runs the portable Core
gate as well. Desktop PASS must not be inferred from a headless state PASS.

Remaining qualification: real lane hotkey collisions/AltGr, hover audio/fade,
family selection, English direct composition, Notepad Smart followup, high
contrast/Narrator, 125/150/200% DPI and multiple monitors. No package, signing,
updater, backend, credential storage or production release changes in this slice.

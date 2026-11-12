# WSA-UX-013_windows_editor_tabs

Initial implementation: 2026-11-09. Cleanup qualification: 2026-11-12.

## Summary

Continue the existing .NET 10 WPF implementation: its activating editor,
non-activating toolbar/overlay, portable Core and common candidate commit path
remain suitable. Add document ownership without rewriting the engine or platform.
macOS is a read-only interaction reference; no macOS code or pack format changes.

## User-visible behavior

Native document tabs have independent text, file paths, dirty indicators,
selection/caret and in-session undo/redo. Ctrl+T/Ctrl+N creates, Ctrl+Tab and
Ctrl+Shift+Tab switch, Ctrl+W closes, Ctrl+O opens, Ctrl+S saves and Ctrl+Shift+S
saves as. Mouse +/close and Filer commands use the same workspace operations.
An already open file activates its existing unsaved buffer. At least one tab
remains. Closing the editor hides it; app exit still offers save/discard/cancel.
Restart recovers tabs, drafts, active document and selection, but not undo history.

## Implementation details

`EditorWorkspace` and `EditorDocumentTab` own document state. `InternalEditorWindow`
retains one native RichTextBox per tab. Switching invalidates suggestion and
analysis generations even for identical text, stops speech/dictation and guards
queued dictation by document ID. Candidate acceptance stays on the shared path.
Quit decisions are transactional: cancelling a later prompt preserves earlier
drafts. Saving cannot overwrite another open tab's file.

`EditorWorkspaceStore` autosaves after 500 ms, flushes on hide/exit and atomically
replaces schema-v1 JSON with a recovery backup. Older write revisions are ignored.
Future, unreadable and structurally malformed same-schema sessions are preserved,
including corruption discovered immediately before Save. The editor shows a calm
warning and permits explicit text-file saves. Limits are 128 tabs, 2 MB opened
files, 2 million UTF-16 units per restored document and 64 MiB session JSON.

Single-instance coordination includes explicit isolated QA profiles. Initial
unsupported external targets now produce a calm status rather than appearing
available. External commit safety and Core language behavior remain unchanged.
The native analysis test now waits for layout/pointer-over before injecting mouse
buttons; its correction, undo, focus and explicit-choice assertions are retained.

## Files changed

- /README.md
- /docs/Architecture.md
- /docs/ManualSmoke.md
- /docs/ParityMatrix.md
- /docs/Plan.md
- /docs/README.md
- /docs/UiParityPlan.md
- /docs/changes/2026-11-09_wsa_ux_013_editor_tabs.md
- /docs/qualification/WSA-UX-013_editor_tabs.md
- /scripts/test_app.ps1
- /scripts/test_gui.ps1
- /src/WordSuggestorWindows.App/InternalEditorWindow.xaml
- /src/WordSuggestorWindows.App/InternalEditorWindow.xaml.cs
- /src/WordSuggestorWindows.App/MainWindow.xaml
- /src/WordSuggestorWindows.App/MainWindow.xaml.cs
- /src/WordSuggestorWindows.App/Models/EditorDocumentTab.cs
- /src/WordSuggestorWindows.App/Services/EditorWorkspace.cs
- /src/WordSuggestorWindows.App/Services/EditorWorkspaceStore.cs
- /src/WordSuggestorWindows.App/Services/SingleInstance.cs
- /src/WordSuggestorWindows.App/Services/WindowsGlobalSuggestionCaptureService.cs
- /src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs
- /tests/WordSuggestorWindows.Smoke/AnalysisSettingsSmoke.cs
- /tests/WordSuggestorWindows.Smoke/EditorTabsSmoke.cs
- /tests/WordSuggestorWindows.Smoke/GuiSmoke.cs
- /tests/WordSuggestorWindows.Smoke/Program.cs

## Feature flags / settings impact

No new setting schema or consent. Drafts and file paths are local user content at
`%LOCALAPPDATA%\WordSuggestor\editor\workspace-v1.json` and `.bak`, separate
from settings, packs, user words and diagnostics. They are plaintext local drafts,
not uploaded. `test_gui.ps1 -EditorOnly` disables external capture only in its
isolated test profile. Existing normal data locations are retained.

## Logging / telemetry impact

No document content, raw keystrokes or paths added to diagnostic output. No new
backend traffic. Synthetic QA drafts/results stay in ignored `.artifacts`.

## Validation performed

See [measured source and desktop evidence](../qualification/WSA-UX-013_editor_tabs.md).
The cleanup added two store-corruption checks; app suite passes 94 including 25
tab/storage checks and stale-result tests. A source-only export builds with zero
warnings/errors, uses real Core and passes 354 Settings/Core checks and six Danish
fixtures. Further desktop results and initial failures are recorded separately.
Developer restart is qualified in the WSA-DX-005 commit.

## Known limitations / follow-up

No public release qualification is inferred. Human Narrator/high contrast,
many-tab visual review, speech while switching, mixed-DPI hardware and signed
N-to-N+1 draft/session retention remain manual gates. Undo is session-only.
No trust-store, production feed or backend changes are needed for tabs.
The next cleanup delivery records the existing beta tooling and tests its local
validation/upload boundaries; actual installation/publication still needs signing
and an approved test environment. No push is performed.

## Consolidation notes for `WordSuggestorDocs`

Windows implementation/evidence lives here. Shared Docs may link this note and
the parity matrix later. Core, macOS, backend and their documentation are untouched.
This supersedes the November 9 uncommitted draft and single-buffer descriptions.

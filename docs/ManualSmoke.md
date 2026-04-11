# WordSuggestorWindows Manual Smoke

Last updated: `2026-04-11`
Owner: `Windows track`

## Goal

Provide one repeatable manual smoke flow for the current Windows baseline:

- bootstrapped `WordSuggestorCore` CLI
- built WPF app
- collapsed floating toolbar startup with sample text ready for manual editor expansion

## Primary commands

Build the Windows app:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build_app.ps1
```

Validate the `WordSuggestorCore` CLI bridge:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test_core_cli.ps1
```

Launch the app in smoke mode:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run_app.ps1
```

Optional custom startup sample:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run_app.ps1 -SampleText "Jeg prøver at skri"
```

## Manual checklist

1. Run `scripts\test_core_cli.ps1` and confirm the CLI returns Danish suggestions, including rows for `læ`, `ø`, `å`, `smør`, and `blå`.
2. Run `scripts\run_app.ps1`.
3. Confirm the app opens as the floating toolbar shell rather than a standard document window.
4. In smoke mode, confirm the shell starts collapsed even when startup text is injected.
5. Open the editor manually and confirm the startup text is already inserted.
6. Confirm the expanded editor now shows a structured command row, one compact status bar, and analyzer legend section.
7. Confirm there is no implementation-note copy under the `Tekstanalyse` label.
8. Confirm the editor field expands vertically to use the available space above the compact status bar and `Tekstanalyse` legend.
9. Confirm the status bar reads as inline metrics such as `Aa 69 tegn`, not separate cards.
10. Confirm a separate floating suggestion overlay appears near the caret rather than inside the editor layout.
11. Confirm the overlay header shows page and count information, and that the status area reports successful suggestion retrieval rather than a bridge error.
12. Confirm the first page can show all 10 visible suggestions without requiring scroll for the default `skri` smoke sample.
13. Confirm the internal editor field keeps a fixed available size, wraps text horizontally, and scrolls vertically when the content exceeds the field height.
14. Confirm words in the internal editor receive visible POS-style color treatment while the `Farver` toggle is active.
15. Confirm each row now shows the suggestion term, an inline type label in parentheses, and a second metadata line when `WordSuggestorCore` returns POS or grammar data.
16. Confirm row backgrounds differ between ordinary, phonetic, misspelling, and synonym suggestions when those candidate kinds are present.
17. Switch the overlay to static placement and drag the header to a new position. Confirm it stays there while typing until you move it again.
18. Switch back to follow-caret and confirm the overlay resumes anchoring under the editor caret when available.
19. Click the speaker button on a row and confirm Windows TTS reads the suggestion aloud.
20. Click the info button on a row and confirm a small info popup appears with match and grammar details.
21. Press `Ctrl+Right` to move to the next page when more than 10 suggestions are available, then `Ctrl+Left` to return.
22. Press `Tab` or `Ctrl+1` to accept the first visible suggestion.
23. Confirm the active token in the editor is replaced, one trailing space is inserted, and the caret is placed after that space.
24. Confirm the floating suggestion overlay clears immediately after accepting the suggestion and stays empty until the next token is typed.

## Expected current behavior

- The app should launch as a floating toolbar shell.
- `scripts\run_app.ps1` should keep the shell collapsed even though it injects the default sample text `Jeg vil gerne skri`.
- Opening the editor manually should reveal the injected startup text.
- The current suggestion UX uses a separate floating overlay window with page controls and placement mode buttons.
- The default first page should fit all 10 visible candidates without needing vertical scrolling, and each row should read as visibly denser than the earlier overlay baseline.
- Each row should now present inline match type, secondary metadata, row-level TTS, and an info affordance.
- The expanded editor should no longer feel like a plain textbox screen; it should expose the same core editor information architecture as macOS.
- The editor input should be a fixed-size rich text surface inside the expanded shell, with vertical scrolling and horizontal wrapping.
- The editor status metrics should appear as one compact status bar, not four separate cards.
- The expanded editor body should stretch to the fixed expanded window height, so unused vertical space is given to the editor input rather than left below the legend.
- The `Tekstanalyse` panel should only show the legend, not implementation-note copy.
- Editor word coloring is currently a Windows-side baseline classifier that mirrors the POS color categories visually; full lexicon-backed analyzer parity remains follow-up work.
- The selected suggestion should be accepted with `Tab`, `Ctrl+1` to `Ctrl+0`, or clicking a suggestion row in the overlay.
- Accepting a suggestion should insert a trailing space unless the following text already starts with whitespace.
- Accepting a suggestion should clear the old suggestion list immediately, so stale candidates are not shown after the caret moves past the inserted space.
- `Ctrl+Left` and `Ctrl+Right` should page the overlay when more than one page of suggestions is available.
- The editor should no longer show separate informational cards above or directly below the text input that duplicate toggle state or overlay placement state.
- If caret anchoring is not available, the overlay should fall back to a stable static position near the editor.
- In static mode, dragging the overlay header should establish the new resting position for the remainder of the session.
- External app typing, caret tracking, and overlay placement are not in scope for this smoke.

## Failure triage

- If the app builds but suggestions fail immediately, run `scripts\test_core_cli.ps1` first.
- If `scripts\run_app.ps1` prints the launch message and immediately returns without opening the app, inspect the Windows Application event log for recent `.NET Runtime`, `Application Error`, or `WordSuggestorWindows.App` entries.
- If CLI build fails on Windows headers or SQLite, rerun `scripts\bootstrap_core_cli.ps1` in a fresh PowerShell session.
- If the app opens but the list is empty, verify the startup text ends with an incomplete Danish token such as `skri`.
- If bootstrap prints `warning: couldn't find pc file for sqlite3` but `scripts\test_core_cli.ps1` returns suggestions and logs `Pack opened ... da_lexicon.sqlite`, the SQLite pack is loading; the warning is from pkg-config discovery during the Swift build.

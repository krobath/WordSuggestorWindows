# WordSuggestorWindows Manual Smoke

Last updated: `2026-04-12`
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
4. Confirm the language selector shows `DA` as the installed Danish baseline language.
5. Open the language selector and confirm the macOS-supported languages are visible; languages without local SQLite packs should show a missing-pack marker such as `!`.
6. Select a missing-pack language and confirm the app reports that the language pack is missing instead of crashing or showing stale Danish suggestions.
7. Select `DA` again and confirm Danish suggestions resume for an incomplete Danish token.
8. Mark text inside the internal editor, click `TXT`, and confirm the editor is replaced with the selected text.
9. In a UI Automation-compatible external app, mark text, then click `TXT` quickly and confirm the recent external selection is imported into the editor.
10. In an external app that does not expose UI Automation selection reliably, mark text, click `TXT`, and confirm the guarded clipboard fallback imports the selected text when the target app accepts `Ctrl+C`.
11. Click `OCR`, select a visible screen region with text, and confirm the recognized text is copied to the clipboard and imported into the internal editor.
12. Confirm the toolbar returns after the screen snip and the editor opens with the OCR text staged for analysis.
13. Click `MIC`, speak a short phrase, and confirm the button shows active state while Windows Speech Recognition is listening.
14. Click `MIC` again and confirm the listening state stops.
15. Confirm final recognized speech is inserted into the internal editor at the caret and that partial/hypothesis text only appears in the status line.
16. If the active app language is Danish but only an English recognizer is installed, confirm the status line reports the recognizer fallback rather than crashing.
17. In smoke mode, confirm the shell starts collapsed even when startup text is injected.
18. Open the editor manually and confirm the startup text is already inserted.
19. Confirm the expanded editor now shows a structured command row, one compact status bar, and analyzer legend section.
20. Confirm there is no implementation-note copy under the `Tekstanalyse` label.
21. Confirm the editor field expands vertically to use the available space above the compact status bar and `Tekstanalyse` legend.
22. Confirm the status bar reads as inline metrics such as `Aa 69 tegn`, not separate cards.
23. Confirm a separate floating suggestion overlay appears near the caret rather than inside the editor layout.
24. Confirm the overlay header shows page and count information, and that the status area reports successful suggestion retrieval rather than a bridge error.
25. Confirm the first page can show all 10 visible suggestions without requiring scroll for the default `skri` smoke sample.
26. Confirm the internal editor field keeps a fixed available size, wraps text horizontally, and scrolls vertically when the content exceeds the field height.
27. Confirm words in the internal editor receive visible POS-style color treatment while the `Farver` toggle is active.
28. Confirm each row now shows the suggestion term, an inline type label in parentheses, and a second metadata line when `WordSuggestorCore` returns POS or grammar data.
29. Confirm row backgrounds differ between ordinary, phonetic, misspelling, and synonym suggestions when those candidate kinds are present.
30. Switch the overlay to static placement and drag the header to a new position. Confirm it stays there while typing until you move it again.
31. Switch back to follow-caret and confirm the overlay resumes anchoring under the editor caret when available.
32. Click the speaker button on a row and confirm Windows TTS reads the suggestion aloud.
33. Click the info button on a row and confirm a small info popup appears with match and grammar details.
34. Press `Ctrl+Right` to move to the next page when more than 10 suggestions are available, then `Ctrl+Left` to return.
35. Press `Tab` or `Ctrl+1` to accept the first visible suggestion.
36. Confirm the active token in the editor is replaced, one trailing space is inserted, and the caret is placed after that space.
37. Confirm the floating suggestion overlay remains visible but empty after accepting the suggestion and stays empty until the next token is typed.
38. Press `Space` or `Enter` after a token and confirm the floating suggestion overlay remains visible but empty.

## Expected current behavior

- The app should launch as a floating toolbar shell.
- The toolbar language selector should expose the macOS-supported language set.
- The selector should route installed packs into the core CLI bridge and mark missing packs without making the app fail.
- In the current workspace, `DA` should be the only installed language because `WordSuggestorCore\Ressources\da_lexicon.sqlite` is the only local pack file.
- The `TXT` toolbar button should import selected text from the internal editor first, then from a recent Windows UI Automation-compatible external selection when available, then by guarded clipboard fallback.
- Clipboard fallback should restore WordSuggestor focus and best-effort restore the previous clipboard contents after the copy attempt.
- App-specific selected-text import behavior should be recorded in `docs/SelectionImportCompatibilityMatrix.md` using the local diagnostic file `%LOCALAPPDATA%\WordSuggestor\diagnostics\selection-import.log` or debugger output lines prefixed with `WordSuggestor selection import:`.
- The `OCR` toolbar button should hide WordSuggestor, start the Windows Snipping Tool protocol capture flow, receive the `wordsuggestor-ocr:` callback, OCR the returned image token, copy the recognized text to the clipboard, and import it into the internal editor.
- OCR troubleshooting should use the local token-safe logs `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log` and `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log`; neither log should contain the recognized OCR text or the callback token.
- Snipping Tool callbacks may provide the redeemable file token as `file-access-token`; WordSuggestor treats that as the shared-storage token and redeems it through the normal token bridge.
- Direct PDF-file OCR import is not implemented yet; visible PDF content should be captured through the screen snip path.
- The `MIC` toolbar button should start/stop a local Windows Speech Recognition bridge, show active button state while listening, and insert final recognized speech into the internal editor at the caret.
- Speech-to-text language support depends on installed Windows Desktop Speech Recognition recognizers. On the current machine the only installed recognizer observed during implementation was `en-GB`, so Danish dictation requires installing a Danish recognizer before language-matched recognition can work.
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
- Accepting a suggestion should clear the old suggestion list immediately while keeping the overlay box visible as an empty panel.
- Boundary input such as `Space` or `Enter` should leave the overlay visible but empty until the next token begins.
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
- If selected-text import works in one app but not another, use `docs/SelectionImportCompatibilityMatrix.md` to record whether UI Automation or clipboard fallback was blocked.
- If OCR returns no text, verify that the callback log contains `token` or `file-access-token`, and that Windows OCR language support is installed for the active user profile.
- If OCR never returns to WordSuggestor after selecting a region, inspect `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log` and confirm the per-user `wordsuggestor-ocr:` protocol handler exists under `HKCU\Software\Classes\wordsuggestor-ocr`.
- If OCR returns to WordSuggestor but the text is not imported into the internal editor, inspect `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log` to find the last successful stage: callback, token bridge, OCR bridge, clipboard copy, or editor import.
- If `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log` shows no `token` or `file-access-token` key for a `code=200` callback, record the query keys in the OCR issue notes because Snipping Tool has changed its callback contract again.
- If `MIC` reports that no Windows Speech Recognition recognizer is installed, install a Windows Desktop Speech Recognition recognizer for the user profile and retry.
- If `MIC` reports `E_ACCESSDENIED` or `Access is denied` from the speech bridge, check Windows microphone/privacy permissions and whether Windows Speech Recognition is available in the active desktop session.
- If `MIC` listens but inserts no text, verify the default microphone input device and check whether the recognizer language matches the spoken language.

# WordSuggestorWindows Manual Smoke

Last updated: `2026-04-14`
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
17. Mark text inside the internal editor, click `TTS`, and confirm the selected text is read aloud while the `TTS` button shows active state.
18. Click `TTS` while speech is active and confirm playback stops.
19. Mark text in an external app, click `TTS`, and confirm the selected text is mirrored into the internal editor before it is read aloud.
20. Mark text in Edge without clicking WordSuggestor first, press `Ctrl+Alt+T`, and confirm the selected text is mirrored into the internal editor before it is read aloud.
21. Mark text in Microsoft Word, click `TXT`, and confirm the selected text is imported without Word freezing or crashing.
22. Mark text in Microsoft Word, click `TTS`, and confirm the selected text is mirrored into the internal editor and read aloud without Word freezing or crashing.
23. During toolbar or hotkey TTS playback, confirm the active word/token in the internal editor gets a light-blue background and that the highlight clears when playback stops.
24. Confirm `%LOCALAPPDATA%\WordSuggestor\diagnostics\tts-flow.log` is created even if external selection capture fails before speech starts.
25. With no active selection but text staged in the internal editor, click `TTS` and confirm the editor text is read aloud.
26. Open settings, select `DA`, and confirm the `Systemstemme` field reports whether a Danish Windows Desktop voice is installed.
27. If no Danish voice is installed, confirm `TTS` reports a fallback voice and `%LOCALAPPDATA%\WordSuggestor\diagnostics\tts-flow.log` records the fallback reason without logging the spoken text.
28. Turn off `Brug systemets oplæsningsindstillinger`, change `Oplæsningshastighed`, save settings, and confirm TTS still starts.
29. Accept at least one suggestion, click `INS`, and confirm the native Insights window opens.
30. Confirm the Insights window shows local totals and recent events without exposing full editor content beyond the typed/accepted correction pairs.
31. Press `Backspace` and sentence-ending keys in the editor, reopen `INS`, and confirm the totals update.
32. Click the settings gear and confirm the native Windows settings window opens.
33. Confirm the settings categories are visible: `Generelt`, `Ordforslag`, `Tekstanalyse`, `Fejlsporing`, and `Avanceret`.
34. Change a supported setting such as placement mode, POS-farvemarkering, semantic diagnostics, punctuation diagnostics, or error tracking, click `Gem`, and confirm the app reflects the change.
35. Reopen settings and confirm the change persisted from `%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json`.
36. Confirm placeholder-only settings such as fagordslister, sentence examples, and debug/performance toggles are visibly disabled rather than pretending to be implemented.
37. In smoke mode, confirm the shell starts collapsed even when startup text is injected.
38. Open the editor manually and confirm the startup text is already inserted.
39. Confirm the expanded editor now shows a structured command row, one compact status bar, and analyzer legend section.
40. Confirm there is no implementation-note copy under the `Tekstanalyse` label.
41. Confirm the editor field expands vertically to use the available space above the compact status bar and `Tekstanalyse` legend.
42. Confirm the status bar reads as inline metrics such as `Aa 69 tegn`, not separate cards.
43. Confirm a separate floating suggestion overlay appears near the caret rather than inside the editor layout.
44. Confirm the overlay header shows page and count information, and that the status area reports successful suggestion retrieval rather than a bridge error.
45. Confirm the first page can show all 10 visible suggestions without requiring scroll for the default `skri` smoke sample.
46. Confirm the internal editor field keeps a fixed available size, wraps text horizontally, and scrolls vertically when the content exceeds the field height.
47. Confirm words in the internal editor receive visible POS-style color treatment while the `Farver` toggle is active.
48. Confirm each row now shows the suggestion term, an inline type label in parentheses, and a second metadata line when `WordSuggestorCore` returns POS or grammar data.
49. Confirm row backgrounds differ between ordinary, phonetic, misspelling, and synonym suggestions when those candidate kinds are present.
50. Switch the overlay to static placement and drag the header to a new position. Confirm it stays there while typing until you move it again.
51. Switch back to follow-caret and confirm the overlay resumes anchoring under the editor caret when available.
52. Click the speaker button on a row and confirm Windows TTS reads the suggestion aloud.
53. Click the info button on a row and confirm a small info popup appears with match and grammar details.
54. Press `Ctrl+Right` to move to the next page when more than 10 suggestions are available, then `Ctrl+Left` to return.
55. Press `Tab` or `Ctrl+1` to accept the first visible suggestion.
56. Confirm the active token in the editor is replaced, one trailing space is inserted, and the caret is placed after that space.
57. Confirm the floating suggestion overlay remains visible but empty after accepting the suggestion and stays empty until the next token is typed.
58. Press `Space` or `Enter` after a token and confirm the floating suggestion overlay remains visible but empty.

## Expected current behavior

- The app should launch as a floating toolbar shell.
- The toolbar language selector should expose the macOS-supported language set.
- The selector should route installed packs into the core CLI bridge and mark missing packs without making the app fail.
- In the current workspace, `DA` should be the only installed language because `WordSuggestorCore\Ressources\da_lexicon.sqlite` is the only local pack file.
- The `TXT` toolbar button should import selected text from the internal editor first, then from a recent Windows UI Automation-compatible external selection when available, then by guarded clipboard fallback.
- If the toolbar click steals focus from the source app, `TXT` and `TTS` should still reuse the latest valid external selection from that same app window rather than importing unrelated stale text.
- Clipboard fallback should restore WordSuggestor focus and best-effort restore the previous clipboard contents after the copy attempt.
- For Microsoft Word specifically, `TXT` and `TTS` should now resolve selected text through the Word COM adapter instead of clipboard fallback.
- `selection-import.log` should show an `OfficeSelection` success path for working Word runs and should not show clipboard-copy success against `WINWORD.EXE`.
- App-specific selected-text import behavior should be recorded in `docs/SelectionImportCompatibilityMatrix.md` using the local diagnostic file `%LOCALAPPDATA%\WordSuggestor\diagnostics\selection-import.log` or debugger output lines prefixed with `WordSuggestor selection import:`.
- For Microsoft Word or other Office apps, capture the enriched `selection-import.log` lines after each `TXT`/`TTS` attempt; they now include target hwnd, pid, process name, responding state, window title/class, route choice, and clipboard fallback timings where fallback still applies.
- The `OCR` toolbar button should hide WordSuggestor, prefer a legacy `ms-screenclip:` rectangle snip so the user sees the cross-hair overlay, OCR the captured clipboard image, copy the recognized text to the clipboard, and import it into the internal editor.
- If the legacy screenclip launch is unavailable, OCR should fall back to the `wordsuggestor-ocr:` callback/token flow instead of silently failing.
- OCR troubleshooting should use the local token-safe logs `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log` and `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log`; neither log should contain the recognized OCR text or the callback token.
- OCR callback persistence may now fall back to `%TEMP%\WordSuggestor\ocr-callbacks` if `%LOCALAPPDATA%\WordSuggestor\ocr-callbacks` is not writable on the current machine.
- Snipping Tool callbacks may provide the redeemable file token as `file-access-token`; WordSuggestor treats that as the shared-storage token and redeems it through the normal token bridge.
- The current OCR launch request intentionally uses a minimal `ms-screenclip` contract so Snipping Tool enters active rectangle snip mode more reliably on current Windows builds.
- The current OCR implementation now prefers a legacy `ms-screenclip:` compatibility path first because it produces the expected overlay/cross-hair experience more reliably on the current machine.
- Direct PDF-file OCR import is not implemented yet; visible PDF content should be captured through the screen snip path.
- The `MIC` toolbar button should start/stop a local Windows Speech Recognition bridge, show active button state while listening, and insert final recognized speech into the internal editor at the caret.
- Speech-to-text language support depends on installed Windows Desktop Speech Recognition recognizers. On the current machine the only installed recognizer observed during implementation was `en-GB`, so Danish dictation requires installing a Danish recognizer before language-matched recognition can work.
- The `TTS` toolbar button should read internal editor selection first, then live external selection, last known external target window selection, recent external selection, guarded clipboard fallback selection, then staged internal editor text.
- External selected text read through `TTS` should be mirrored into the internal editor before playback so the user has visible reading context.
- Pressing `Ctrl+Alt+T` while WordSuggestor is running should trigger the same TTS flow without first clicking the toolbar, which is the preferred path when Windows foreground focus matters.
- While toolbar/hotkey TTS is active, the internal editor should show a light-blue active-token reading highlight and clear it when playback stops.
- The editor should restore the previous caret or selection when TTS stops.
- If `ReadingHighlightMode` is set to `none`, playback should run without moving highlight in the editor.
- If `ReadingHighlightMode` is set to `sentence`, the Windows editor should highlight sentence-sized ranges instead of single words.
- With OneCore playback active, word or sentence highlighting should now follow speech-boundary metadata rather than a rough total-duration estimate.
- With OneCore playback active, the first spoken word should highlight the first spoken word in the editor rather than a word much later in the text.
- The precise OneCore playback bridge should tolerate ordinary Danish punctuation and apostrophes in the spoken text without failing before playback starts.
- Clicking `TTS` while playback is active should stop the current playback.
- Toolbar TTS should now prefer an explicitly selected OneCore or SAPI voice for the active WordSuggestor language, then any installed matching voice, then a visible fallback voice if no language match is installed.
- TTS diagnostics should be written to `%LOCALAPPDATA%\WordSuggestor\diagnostics\tts-flow.log` without storing the spoken text.
- Latest manual validation on `2026-04-14` confirms the Danish voice can be selected in settings and used for playback on this machine.

## Current WSA-RT-013D status

- `WSA-RT-013D` now covers OneCore voice discovery, source-aware voice selection, and OneCore-first runtime dispatch with SAPI fallback.
- The current machine exposes `Microsoft Helle - Danish (Denmark)` under `Speech_OneCore`.
- Latest manual validation indicates that Danish playback now works on this machine.
- Remaining TTS parity work is concentrated around playback highlighting and external-app polish.
- OneCore highlight timing is now metadata-driven; any remaining timing mismatch should be investigated as a bridge/runtime bug rather than as the old estimate-based scheduler.
- The current precise OneCore bridge now uses temp script/payload artifacts rather than one large inline command, which was the earlier parser-failure source.
- Original sprint contract notes are retained below for traceability.
- Its validation target is that `Microsoft Helle - Danish (Denmark)` becomes visible in `Generelt > Oplæsning` for `DA` and can be used for actual toolbar playback.
- `SAPI Desktop` fallback is still required while OneCore playback is being validated in the current WPF host.
- The `INS` toolbar button should open a native Windows Insights window backed by local data from `%LOCALAPPDATA%\WordSuggestor\insights\error-insights.jsonl`.
- Insights should update after accepted suggestions, backspace activity, and sentence boundary input in the internal editor.
- Insights data is local-only in the current baseline and should not be written to `WordSuggestorCore` or macOS app state.
- The settings gear should open a native Windows settings window with macOS-aligned categories and local persistence under `%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json`.
- Settings for language, suggestion placement, global suggestions, analyzer coloring, semantic diagnostics, punctuation diagnostics, error tracking, and system speech preference should be persisted and applied to the running Windows session where the current runtime supports them.
- Settings should show system voices filtered by the active WordSuggestor language and provide shortcuts to Windows speech/language settings when the matching voice is missing.
- Settings that are not yet backed by Windows runtime behavior should be shown as disabled placeholder parity controls.
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
- If OCR hides WordSuggestor and then appears to do nothing for about 90 seconds, inspect `ocr-flow.log` for `Capture stopped: no callback before timeout.` and confirm whether a callback file was written under either `%LOCALAPPDATA%\WordSuggestor\ocr-callbacks` or `%TEMP%\WordSuggestor\ocr-callbacks`.
- If OCR shows no cross-hair overlay, inspect `ocr-flow.log` for `Launching legacy screenclip URI:` first. If that line is missing, the OCR flow never reached the compatibility launch path.
- If the legacy screenclip path launches but no image arrives, inspect `ocr-flow.log` for `Waiting for clipboard image from legacy screenclip path.` and whether the clipboard image was persisted to a temp PNG.
- If Snipping Tool opens but does not enter active snip mode on the modern fallback path, inspect `ocr-flow.log` for the exact `Launching Snipping Tool URI:` line and compare it with the current Microsoft `ms-screenclip` documentation before adding optional parameters back.
- If OCR returns to WordSuggestor but the text is not imported into the internal editor, inspect `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log` to find the last successful stage: callback, token bridge, OCR bridge, clipboard copy, or editor import.
- If `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log` shows no `token` or `file-access-token` key for a `code=200` callback, record the query keys in the OCR issue notes because Snipping Tool has changed its callback contract again.
- If `MIC` reports that no Windows Speech Recognition recognizer is installed, install a Windows Desktop Speech Recognition recognizer for the user profile and retry.
- If `MIC` reports `E_ACCESSDENIED` or `Access is denied` from the speech bridge, check Windows microphone/privacy permissions and whether Windows Speech Recognition is available in the active desktop session.
- If `MIC` listens but inserts no text, verify the default microphone input device and check whether the recognizer language matches the spoken language.
- If `TTS` cannot find text to read, verify there is either an internal editor selection, a compatible external selection, or staged text in the internal editor.
- If `TTS` works for internal text but not external text, record the target app in `docs/SelectionImportCompatibilityMatrix.md` because toolbar TTS reuses the same external selection adapters as `TXT`.
- If WordSuggestor crashes around external selection polling, inspect the Windows Application log for `UIAutomationClientSideProviders`; `WSA-RT-010B` is intended to convert that class of polling failure into diagnostics instead of a crash.
- If `TTS` starts but uses the wrong language, inspect `%LOCALAPPDATA%\WordSuggestor\diagnostics\tts-flow.log` and confirm a Windows Desktop voice is installed for the active language.
- If `TTS` starts and then freezes or crashes during highlight movement, inspect the latest `Application Error` / `Windows Error Reporting` entries for `PresentationCore` or `System.OutOfMemoryException`; `WSA-RT-013H` removed the earlier per-cue document background mutations specifically to reduce this WPF pressure.
- If no voice is listed for Danish, open Windows speech/language settings from Settings > `Generelt` > `Oplæsning` and install a Danish voice before retesting.
- If `INS` opens but shows no accepted-suggestion data, accept a suggestion through `Tab`, `Ctrl+1` through `Ctrl+0`, or overlay click first, then reopen the Insights window.
- If `INS` fails to reflect new local events, inspect `%LOCALAPPDATA%\WordSuggestor\insights\error-insights.jsonl` for append failures or malformed JSONL rows.
- If settings do not persist, inspect `%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json` and verify the current Windows user has write access to `%LOCALAPPDATA%\WordSuggestor\settings`.
- If a settings control is disabled, treat it as documented placeholder parity unless the relevant Windows runtime feature has been implemented in a later sprint.

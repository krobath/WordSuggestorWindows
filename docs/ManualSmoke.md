# WordSuggestorWindows Manual Smoke

Last updated: `2026-04-10`
Owner: `Windows track`

## Goal

Provide one repeatable manual smoke flow for the current Windows baseline:

- bootstrapped `WordSuggestorCore` CLI
- built WPF app
- startup sample text ready for floating-overlay suggestion verification

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

1. Run `scripts\test_core_cli.ps1` and confirm the CLI returns Danish suggestions.
2. Run `scripts\run_app.ps1`.
3. Confirm the app opens as the floating toolbar shell rather than a standard document window.
4. In smoke mode, confirm startup text is already inserted and the shell opens expanded into the internal editor.
5. Confirm the expanded editor now shows a structured command row, editor header/status strip, status metric cards, and analyzer legend section.
6. Confirm a separate floating suggestion overlay appears near the caret rather than inside the editor layout.
7. Confirm the overlay header shows page and count information, and that the status area reports successful suggestion retrieval rather than a bridge error.
8. Confirm the first page can show all 10 visible suggestions without requiring scroll for the default `skri` smoke sample.
9. Confirm the placement buttons switch between static placement and follow-caret mode.
10. Press `Ctrl+Right` to move to the next page when more than 10 suggestions are available, then `Ctrl+Left` to return.
11. Press `Tab` or `Ctrl+1` to accept the first visible suggestion.
12. Confirm the active token in the editor is replaced and the caret remains in the editor.

## Expected current behavior

- The app should launch as a floating toolbar shell.
- `scripts\run_app.ps1` should open the shell expanded because it injects the default sample text `Jeg vil gerne skri`.
- The current suggestion UX uses a separate floating overlay window with page controls and placement mode buttons.
- The default first page should fit all 10 visible candidates without needing vertical scrolling, and each row should read as visibly denser than the earlier overlay baseline.
- The expanded editor should no longer feel like a plain textbox screen; it should expose the same core editor information architecture as macOS.
- The selected suggestion should be accepted with `Tab`, `Ctrl+1` to `Ctrl+0`, or clicking a suggestion row in the overlay.
- `Ctrl+Left` and `Ctrl+Right` should page the overlay when more than one page of suggestions is available.
- If caret anchoring is not available, the overlay should fall back to a stable static position near the editor.
- External app typing, caret tracking, and overlay placement are not in scope for this smoke.

## Failure triage

- If the app builds but suggestions fail immediately, run `scripts\test_core_cli.ps1` first.
- If CLI build fails on Windows headers or SQLite, rerun `scripts\bootstrap_core_cli.ps1` in a fresh PowerShell session.
- If the app opens but the list is empty, verify the startup text ends with an incomplete Danish token such as `skri`.

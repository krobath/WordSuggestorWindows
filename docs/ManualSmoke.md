# WordSuggestorWindows Manual Smoke

Last updated: `2026-04-09`
Owner: `Windows track`

## Goal

Provide one repeatable manual smoke flow for the current Windows baseline:

- bootstrapped `WordSuggestorCore` CLI
- built WPF app
- startup sample text ready for in-app suggestion verification

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
5. Confirm the live suggestion preview populates without typing another character.
6. Confirm the status area reports successful suggestion retrieval rather than a bridge error.
7. Press `Tab` or `Ctrl+1` to accept the first suggestion.
8. Confirm the active token in the editor is replaced and the caret remains in the editor.

## Expected current behavior

- The app should launch as a floating toolbar shell.
- `scripts\run_app.ps1` should open the shell expanded because it injects the default sample text `Jeg vil gerne skri`.
- The current suggestion UX is a temporary in-app preview strip, not the final floating overlay panel.
- The selected suggestion should be accepted with `Tab`, `Ctrl+1` to `Ctrl+0`, or clicking a suggestion chip.
- External app typing, caret tracking, and overlay placement are not in scope for this smoke.

## Failure triage

- If the app builds but suggestions fail immediately, run `scripts\test_core_cli.ps1` first.
- If CLI build fails on Windows headers or SQLite, rerun `scripts\bootstrap_core_cli.ps1` in a fresh PowerShell session.
- If the app opens but the list is empty, verify the startup text ends with an incomplete Danish token such as `skri`.

# WSA-RT-006_windows_overlay_crash_and_danish_utf8

Date: `2026-04-11`
Status: `Done`

## Summary

Fixed a WPF overlay crash triggered when suggestions rendered after short input, and made Danish letters part of the Windows bridge regression path.

## User-visible behavior

- Typing two letters should no longer crash the Windows app when the suggestion overlay appears.
- The app bridge now explicitly uses UTF-8 for suggestion input/output, so `æ`, `ø`, and `å` are not dependent on the Windows console codepage.
- The smoke script now exercises Danish-letter inputs in addition to the existing `skri` baseline.

## Implementation details

- Changed the `Run.Text` bindings for overlay row term and inline kind label to `Mode=OneWay`; WPF otherwise treated the binding as a source-updating binding against a read-only computed property and threw `XamlParseException`.
- Updated `WordSuggestorCoreCliSuggestionProvider` to write temp input files with `Encoding.UTF8` and read redirected CLI stdout/stderr as UTF-8.
- Corrected Danish overlay labels touched by the prior overlay iteration.
- Updated `scripts/test_core_cli.ps1` to write sample inputs with UTF-8 encoding and include `læ`, `ø`, `å`, `smør`, and `blå`.

## Files changed

- `WordSuggestorWindows/scripts/test_core_cli.ps1`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/SuggestionPresentation.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml.cs`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/Plan.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Direct `WordSuggestorSuggestCLI` smoke with `læ`, `ø`, `å`, `smør`, and `blå` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap -SampleText "Jeg prøver at læ"` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- Recent Application event log check after launch showed no new `WordSuggestorWindows.App` crash event.

## Follow-up

- Return to visual parity adjustments after this stability fix, especially the overlay chrome and row proportions versus macOS.

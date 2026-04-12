# WSA-RT-009_windows_language_pack_selection

## Summary

Implemented the first Windows language selector parity pass so the top toolbar no longer exposes Danish as a hardcoded-only option.

## User-visible behavior

- The toolbar language selector now shows the macOS-supported language set.
- Danish resolves to the installed legacy pack in the current workspace.
- Languages without installed SQLite packs remain visible but show a missing-pack marker in the compact selector label.
- Selecting a missing-pack language is safe: the app reports that the language pack is missing and keeps the suggestion overlay empty instead of attempting a failing CLI request.

## Implementation details

- Added `LanguageOption` to model:
  - BCP-47 language code
  - compact toolbar label
  - display label
  - pack tag
  - legacy pack filename
  - resolved pack path
- Extended `ISuggestionProvider` so the UI can read language options, inspect the selected language, and set the active language.
- Reworked `WordSuggestorCoreCliSuggestionProvider` to discover packs from:
  - `%APPDATA%\WordSuggestor\Packs`
  - `WordSuggestorWindows\Packs`
  - `WordSuggestorCore\Ressources`
- Added support for current pack naming and legacy names:
  - `<tag>_pack_v*.sqlite`
  - `<tag>_pack.sqlite`
  - `da_lexicon.sqlite`
  - `en_lexicon.sqlite`
- Updated CLI invocation so `--lang` and `--pack` come from the selected `LanguageOption`.
- Updated `MainWindowViewModel` so selecting a missing-pack language clears suggestions safely and selecting an installed language refreshes suggestions when the editor has an active token.
- Updated the toolbar `ComboBox` to bind to `LanguageOption` objects and show compact labels such as `DA` or `EN !`.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/Models/LanguageOption.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/ISuggestionProvider.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/UiParityPlan.md`
- `WordSuggestorWindows/docs/ParityMatrix.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-12_wsa_rt_009_windows_language_pack_selection.md`

## Feature flags / settings impact

No feature flags were added. The selected language is runtime state only in this sprint and is not persisted across app restarts yet.

## Logging / telemetry impact

No telemetry was added. The runtime provider summary now includes the active compact language label so support/debug status can show which language the CLI bridge is using.

## Validation performed

- First build attempt caught a missing `System.IO` import in `LanguageOption`.
- Second build attempt was blocked by an already-running `WordSuggestorWindows.App.exe` test process that locked the output executable.
- Stopped the local test process and reran:
  - `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Ran the core bridge smoke:
  - `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- Ran the app launch smoke:
  - `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS`
- Checked the Windows Application event log after launch:
  - no recent `WordSuggestorWindows.App` crash event found

## Known limitations / follow-up

- Only Danish is installed in the current workspace because `WordSuggestorCore\Ressources\da_lexicon.sqlite` is the only local pack file.
- Missing-pack languages are visible and safe, but installing/downloading packs is not part of this sprint.
- Persisting the user's selected language is deferred to the settings/profile parity work.

# WSA-UX-029 — slice 1: Windows Settings sidebar

Date: 2026-11-10. Owner: WordSuggestorWindows. Reference: macOS
`ba18b451372dd2e3f125e557c5db45894f4e1ae3` and the eleven supplied screenshots.

## Summary

Implemented the native WPF Settings shell with eight vertical categories, grouped
rows, contextual help and a separate view of the previous Windows settings.
This is slice 1 of the existing sprint, following the committed slice 0 baseline
`2b9fa83`. Full functional parity across the 72-control register remains open.

## User-visible behavior

- Profile opens first, followed by Generelt, Oplæsning, Ordforslag, Tekstanalyse,
  Læring, Fejlsporing and Avanceret. Only the right content scrolls.
- Info buttons open one explanation at a time. Clicking the same button, Escape,
  closing help or switching category dismisses it. Escape/close returns focus to
  the originating info button; category selection retains navigation focus.
- Avanceret → Åbn legacy-indstillinger opens the previous Windows view, once per
  Settings window. It has no recursive legacy action. Existing speech choices,
  language, placement, preferences, version and manual update check are retained.
- Gem commits a draft; Annuller/Escape discards it. Live saving is slice 2 work.
  Failed or conflicting saves keep the dialog and draft open with a calm message.
- Account/learning pages explain their unavailable state. Hello cannot be enabled;
  automatic update authority is explicitly Windows-owned. Reading strategy is
  disabled in the primary view because automatic reading while typing has no
  Windows runtime yet. Its stored value and the old editable legacy control remain.

## Implementation details

WPF/.NET 10 and the existing solution are retained. No engine, language-pack,
backend, package-identity or signing changes are introduced.

`SettingsRow` and `SettingsStyles` provide shared label/help/value columns and
system colors. Native ListBox navigation supplies Windows selection, arrows,
keyboard focus and UI Automation. Read-only values expose their actual text to
UIA, including the running version. `SettingsHelpCoordinator` owns one accessible
popup, dismisses it on owner deactivation/close, and restores focus deliberately.

`SettingsFormController` drives both layouts. The legacy XAML is copied from the
pre-slice working implementation, including its Version/updates tab. The existing
uncommitted `WindowsUpdateService` and its 13 contract checks are included as a
necessary dependency to retain that working behavior; release/uploader work is
excluded. Metadata checks do not install or attest a signed update.

`SettingsPatch` applies only fields changed since a dialog opened, against the
latest stored snapshot. It recursively merges per-language voice overrides and
preserves unknown current-schema properties through JsonExtensionData. Different
concurrent edits of the same field fail rather than silently overwrite. The
existing atomic replace/backup and future-schema protection remain in use.
Corrupt/latest unsupported data is never replaced by a stale Settings draft.
This is in-process dialog concurrency protection, not a cross-process file lock.

Opening a form no longer removes an unavailable saved voice ID: a voice override
is changed only after an explicit voice selection. UI and writing language are
not yet separate persisted authorities. The previous Windows speech-rate scale
is retained pending the documented migration in slice 4.

## Files changed

All paths below are relative to the Windows repository root. Existing unrelated
editor/release modifications remain outside this commit.

- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/SettingsStyles.xaml`
- `/src/WordSuggestorWindows.App/LegacySettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/LegacySettingsWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/Controls/SettingsRow.cs`
- `/src/WordSuggestorWindows.App/Controls/SettingsHelpCoordinator.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsPatch.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsAppSettingsService.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateService.cs`
- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs` (Settings changes only)
- `/tests/WordSuggestorWindows.Smoke/Program.cs` (Settings dispatch and update dependency only)
- `/tests/WordSuggestorWindows.Smoke/SettingsStateSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsUiSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/UpdateSmoke.cs`
- `/scripts/test_settings_app.ps1`
- `/README.md`
- `/docs/README.md`
- `/docs/Plan.md`
- `/docs/Architecture.md`
- `/docs/ParityMatrix.md`
- `/docs/UiParityPlan.md`
- `/docs/ManualSmoke.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/docs/qualification/WSA-UX-029_slice1.md`
- `/docs/changes/2026-11-10_wsa_ux_029_slice1_settings_sidebar.md`

The prior working-tree `GuiSmoke.cs` update-settings route was adjusted to the new
General category for regression testing. That file also contains substantial
pre-existing editor work, so it is retained unstaged. The independent Settings UI
suite is included in this delivery and also runs on the isolated commit export.

## Feature flags / settings impact

Schema stays v1 in `%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json` with
`.bak` on replacement. No data move, defaults from screenshots, consent, upload,
account credentials or experimental feature enablement. Unknown v1 fields and
inactive preferences survive. Explicit Gem/Annuller is a temporary slice boundary,
not the final macOS live-save contract. A08 is implemented; other control IDs
retain their primary-slice qualification requirements.

## Logging / telemetry impact

No new production telemetry or raw document/credential logging. Test profiles and
rendered images contain only synthetic values in ignored `.artifacts/tests`.
Screenshots containing the user's account remain outside git.

## Validation performed

See [slice 1 qualification](../qualification/WSA-UX-029_slice1.md) for exact
commands, source scope and remaining manual gates. State tests cover independent
and conflicting saves, per-language edits/removal, future/corrupt data, backups and
unknown fields. Desktop tests use actual WPF windows and native mouse/keyboard,
plus UIA queries. Full app regression also exercises real Notepad and Core.
The isolated staged-source export independently passed build, fresh Core/SQLite,
app regression, 20 state/37 Settings desktop checks, 25 baseline GUI checks,
three media and 35 qualification contract checks. The final documentation records
the unchanged qualified source/test/script subtrees. Staged diff review found
only the 28 intended text/source files, valid added local links, and no added
private-key/token patterns, machine-specific paths or generated binaries.

Two defects found during testing were fixed: reopening help on a repeated info
click, and row labels hiding the version value from UI Automation. Neither test
expectation was weakened.

## Known limitations / follow-up

Slice 2 will make ordinary preferences take effect as they are changed, separate
interface language from writing language and safely migrate persisted settings.
This will make language changes predictable while the user keeps writing.
Account/Hello, source/domain/analysis/learning controls, configurable shortcut
lanes and OS update preference remain gated by their later slices. There is no
production release or new backend functionality in this change.

Human Narrator/NVDA, high contrast, mixed DPI and multiple monitors are still open.
The automated tests and visual review do not count as that manual sign-off.
Positive Hello and OS-update qualification from slice 0 remains open.

## Consolidation notes for WordSuggestorDocs

Windows owns this implementation, tests, sprint status and qualification. Shared
docs may later link this note and record the shell as implemented, while retaining
the explicit open functional/platform gates. Do not infer macOS changes, account
support or release readiness. No shared documentation repo was modified.

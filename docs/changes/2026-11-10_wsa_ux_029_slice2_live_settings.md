# WSA-UX-029 — slice 2: live Settings and independent interface language

Date: 2026-11-10. Owner: WordSuggestorWindows. Base: `33923af` plus the preserved
working tree. macOS reference: `ba18b451372dd2e3f125e557c5db45894f4e1ae3`.

## Summary

Implemented live preference saving, versioned settings migration, Danish/English
Settings localisation and the separate Follow caret permission. WPF, Core JSON v1,
language packs, user words and the existing release architecture are retained.
Only the Windows repository is changed. Full Settings functional parity, account,
Hello and OS update authority remain later-slice work.

## User-visible behavior

- The primary Settings window saves ordinary preferences as they change and has
  one Close action. Closing or pressing Escape does not undo saved preferences.
  Escape in an open native dropdown closes that dropdown first.
- Interface language offers Follow Windows, Danish and English. All eight Settings
  categories, rows, explanations, option labels, statuses and the Windows legacy
  view change immediately. This does not select a different writing language,
  language pack or voice. Unsupported Windows UI languages fall back to English.
- Writing language uses the same authority as the toolbar/provider. Missing packs
  are shown as unavailable and cannot be chosen through Settings/the toolbar.
- Follow caret permission off forces static placement, hides the overlay choice
  and stops new caret-geometry requests. Enabling it only exposes the choice;
  explicit selection in the overlay is still required. Uncertain geometry retains
  the existing static fallback. Stale in-flight measurements cannot be applied.
- The legacy window retains explicit Save/Cancel. Its saved independent changes
  update the primary window; stale overlapping edits report a conflict and keep
  the legacy draft open. Primary live failures revert the control and do not
  change runtime state, queue a retry or report success.

## Implementation details

`WindowsAppSettingsService` remains the single in-process disk authority. Successful
atomic writes notify the view model, which applies the persisted snapshot once.
`SettingsFormController` uses that notification for the live view and retains the
original baseline for the legacy draft. Toolbar language/global preference changes
also persist successfully before changing the runtime. Service notifications run
on the caller/UI thread; there is no new cross-process writer or background sync.

Schema 2 adds `interfaceLocalePreference` (`system_default`, `da-DK`, `en-GB`) and
`followCaretOptionEnabled`. The historical filename remains
`%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json`; its filename does not
determine its schema. V1 loads migrate in memory only. The first successful write
creates an exact once-only `.v1.bak`, including original encoding bytes, then uses
the existing atomic replace and rolling `.bak`. Writing language, voice IDs,
inactive preferences, explicit false values and unknown properties survive. No
learning/sharing consent is inferred. The previous effective speech-rate bounds
are used consistently by both forms; the macOS speed-policy migration is slice 4.

Future, invalid or unreadable data is preserved and blocks writes. A fresh launch
with unavailable settings uses safe defaults with external capture, error tracking,
text examples and caret permission off. Internal offline writing remains available.
An already running session retains its last successfully applied settings when a
new write fails. Reopening Settings presents that same applied snapshot alongside
the unavailable-data warning, rather than showing fresh defaults that contradict
the running capture state. See recovery/rollback in the qualification note.

`SettingsText` resolves the interface independently of writing language and installs
155 matched, embedded resource keys. Dynamic resources update existing controls;
help/accessibility names and category headings refresh without recreating windows.
The catalogues are embedded with explicit names and `WithCulture=false`, so their
availability does not depend on machine-installed resource files or satellite lookup.

`MainWindow` gates local/external caret geometry using `RequestsCaretGeometry` and
a measurement generation. Existing focus, target verification, commit and Core
generation checks remain in place. No macOS AX code or C# linguistic rules are added.

## Files changed

Paths are relative to the Windows repository root; partial files include only this
slice's changes in the commit. Earlier editor/release work remains outside it.

- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/Models/SettingsSchema.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsAppSettingsService.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsPatch.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsText.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateService.cs`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/Controls/SettingsRow.cs`
- `/src/WordSuggestorWindows.App/Controls/SettingsHelpCoordinator.cs`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/LegacySettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/SharedStyles.xaml`
- `/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml`
- `/src/WordSuggestorWindows.App/MainWindow.xaml.cs` (caret guards only)
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs` (Settings authority only)
- `/src/WordSuggestorWindows.App/WordSuggestorWindows.App.csproj`
- `/tests/WordSuggestorWindows.Smoke/SettingsStateSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsUiSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/GuiSmoke.cs` (explicit test locale and caret-permission checks)
- `/README.md`
- `/docs/README.md`
- `/docs/Plan.md`
- `/docs/Architecture.md`
- `/docs/ParityMatrix.md`
- `/docs/UiParityPlan.md`
- `/docs/ManualSmoke.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/docs/qualification/WSA-UX-029_slice2.md`
- `/docs/changes/2026-11-10_wsa_ux_029_slice2_live_settings.md`

## Feature flags / settings impact

Device-local schema 2 only; no backend setting, new sharing consent, account token,
pack change or data relocation. English interface does not require an English
language pack. Existing installed compatible packs alone govern writing language.
G01–G03 have the Windows Settings behavior above; full application-wide translation
is not claimed. The older toolbar/editor and some secondary dialog/runtime texts
still use their existing Danish copy. G05 remains Windows-owned, with no false
local automatic-update checkbox. The 72-control register stays frozen as slice 0
history; later runtime qualification is recorded separately.

## Logging / telemetry impact

No new production telemetry, document logging or credentials. Failures expose calm
UI status without raw exception text. Isolated tests use synthetic settings, text
and screenshots in ignored `.artifacts`; account reference screenshots remain out
of git. Stopping geometry work does not disable text/selection reads necessary for
the verified external commit adapter.

## Validation performed

[Slice 2 qualification](../qualification/WSA-UX-029_slice2.md) contains exact
commands, scope, results, source evidence and manual smoke. State tests cover raw
V1 backup, migration/defaults, consent preservation, future/corrupt formats,
localisation, pack rejection, conflicts and runtime rollback. Actual-window tests
cover live saving, UI language switches, Danish Core with English Settings,
legacy concurrency, real file-sharing failures and dropdown Escape behavior.
The real app/Notepad suite verifies the permission and overlay choice end to end.
The final isolated candidate passed a zero-warning/error build, fresh Core/SQLite
and six fixtures, 67 app checks, 63 Settings state checks, 58 Settings desktop
checks, 30 app/Notepad GUI checks, three media and 35 qualification safety checks.
The unchanged qualified source/test/script subtrees are recorded in the report.
Earlier 92-app/47-GUI working-tree results include preserved uncommitted editor
work and are not attributed to this commit alone.

Tests caught an embedded-resource deployment error, which was fixed with explicit
manifest resource names. The concurrent-window fixture was corrected to put the
owned legacy window beside its owner and verify actual mouse hit targets, rather
than clicking through an overlapping window. Expectations were not weakened.
The export wrapper was also corrected to use the actual PowerShell location and
assert build output scope; its earlier wrong-root run is excluded from clean-source
evidence. Final staging review covers only the 32 intended text/source files and
their links; binaries, keys, tokens and machine paths are not added.

## Known limitations / follow-up

Localisation is complete for the Settings/legacy surfaces, not all historical app
dialogs. Human Narrator/NVDA, high contrast, mixed DPI/multiple monitors, physical
Hello and signed installed update/rollback remain unqualified. No new production
publication or installer qualification is implied. Already executing native caret
reads may finish, but their result is rejected after permission/placement changes.

Next, slice 3 will define and qualify what the shared Core bridge can actually do
for suggestion sources, domain lists, word families, analysis and learning signals.
Users will then be able to receive controls backed by real shared engine results;
until those capabilities are implemented and tested, their controls stay unavailable.
No separate C# ranking implementation, fake analysis or new backend protocol is allowed.

## Consolidation notes for WordSuggestorDocs

Later shared-doc consolidation may link this Windows note and record schema 2,
live Settings, the independent interface preference and verified caret permission.
Keep the partial translation boundary, later functional slices and platform/release
gates visible. No shared or macOS documentation was modified.

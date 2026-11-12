# WordSuggestorWindows architecture

## Windows update boundary — 2026-11-12

Retain WPF/Core/pack and extend the existing update adapter. Typed WinRT lives in
`WindowsNativeUpdatePlatform`; immutable OS snapshots and a single-flight
`WindowsUpdateCoordinator` separate native authority, bounded HTTPS metadata and
Settings UI. `SettingsFormController.Updates` renders actual OS state; no app JSON
bool represents scheduling. `WindowsUpdateHandoff` uses normal owner/root close
and the existing unsaved-document guard before a fixed OS installer URI is opened.
Tickets are single-use and expire after two minutes. No custom installer is added.

The managed SDK is pinned to 10.0.22621.57, compile TFM to
`net10.0-windows10.0.22621.0`, minimum API baseline 19041. Existing build output
paths remain compatible. Production policy writes are explicitly source-gated
off until the packaged native acceptance gate passes. See [Windows update contract](WindowsUpdates.md) and [slice-8 evidence](qualification/WSA-UX-029_slice8.md).

## Account boundary — 2026-11-11

Retain WPF and Core/pack architecture. `AccountSessionCoordinator` owns serialized,
cancellable backend account operations and verified refresh identity/expiry.
`AccountBackendClient` uses the existing snake_case contracts and strict HTTPS;
`AccountSessionVault`/`WindowsAccountCredentials` isolate local secret persistence.
`WindowsHelloSessionKey` isolates protected native signing from app orchestration.
`AccountSettingsView` owns profile drafts and transactions without changing common
Settings schema or Core ranking. See [account/session/Hello contract](AccountAndHello.md) and [slice-7 qualification](qualification/WSA-UX-029_slice7.md). Native Hello is not yet qualified
on this host; account endpoints are tested with synthetic HTTP fixtures.

## Document analysis and learning boundary — 2026-11-11

`MainWindowViewModel.Analysis` coalesces/cancels Core document requests and verifies
original text, language and generation. `AnalysisRangeAdorner` paints without
editing formatting or selection. `DiagnosticEditPlanner` validates ranges/groups;
the editor applies one native undo transaction only to its verified original target.
Menus suppress the overlay and return focus only from the verified owned action.
`WindowsWordChoiceStore` atomically stores opt-in interpretation choices separately
from user words/insights, with no ranking or upload. Schema 5 preserves prior data.
See [full data/failure/engine contract](AnalysisAndLearning.md) and
[qualification](qualification/WSA-UX-029_slice6.md).

## Suggestion settings boundary — 2026-11-11

Retain the existing WPF shell, activating editor, nonactivating overlay and CLI.
The [suggestion contract](Suggestions.md) describes schema 4, cloned preferences,
source-backed v2 plans, three lane collections and cancellation/generation checks.
Mouse/keyboard/family selection share one commit; external requests retain the
original control/window/PID/process-start/text/selection snapshot. Native exact
edits reverify it rather than retargeting a newer field in the same window.
No C# ranking/punctuation heuristics, clipboard fallback or extra backend flow.
The [slice-5 follow-up](qualification/WSA-UX-029_slice5.md) records native
qualification on the newer slice-6 source, including three lanes and Notepad;
human/hardware signoff remains open.

Updated 2026-11-08. Decision: controlled refactoring of the functioning WPF app,
upgraded to .NET 10 LTS. No new solution or WinUI rewrite. See [audit](ReleaseAudit.md)
for the compared options and [Core contract](BridgeContract.md) for wire details.

## Speech lifecycle — WSA-UX-029 slice 4

`MainWindow.Speech` orchestrates typed document/verified native-target snapshots
and shared cancellable playback. Existing WPF/OneCore/SAPI adapters remain; ranking,
normalization and pack logic stay in Core. Speech trigger/span policy is bounded
presentation logic, not a second grammatical analyzer. Text changes, target/voice/
strategy changes and window shutdown cancel stale work. Appending after an already
committed automatic span retains its audio; the next boundary replaces it without
a growing queue. Manual speech stops on editing/navigation/focus change.

The worker is a fixed embedded PowerShell resource, started with the Windows system
PowerShell path. Versioned JSON travels over stdin; numeric cue/audio timing travels
back over bounded pipes. No text-bearing arguments/script/JSON temp files or raw
stderr diagnostics. Each utterance owns its process, opaque event identity and
random scratch directory; cancellation invalidates the identity, kills/reaps the
process tree and removes its script/WAV. Dispatcher delivery checks the identity
again, plus editor reference/generation, before changing presentation.

Native word/sentence metadata replaces estimated highlighting on both routes.
An adorner paints ranges without moving focus, selection, caret or undo. SAPI SSML
offsets/entities map back to the exact original buffer; invalid/surrogate-splitting
metadata is ignored. No voice timing means no invented highlight.
See [speech contract and recovery](Speech.md) and [qualification](qualification/WSA-UX-029_slice4.md).

## Settings engine boundary — WSA-UX-029 slice 3

The existing CLI provider also implements `ICoreSettingsAdapter` through a partial
class and a shared serialized process transport. Additive v1 capabilities precede
typed JSON v2 queries; only an explicit old-protocol rejection allows legacy
fallback. Timeouts, cancellation, bounded output and child-process reaping are
shared with the existing v1 path. No shell or text-bearing diagnostic exception
is introduced. The normal app suggestion path still uses v1.

DTOs preserve Core source switches, separate domain display/insert text, opaque
reading IDs, pack-dependent families and original UTF-16 diagnostic ranges/groups.
Response version/request/document/generation/language must match. Future callers
must additionally use `CoreQueryResult.IsCurrent` against their live document and
language at dispatcher delivery; returning a scoped DTO does not itself update UI.
The adapter rejects invalid ranges and split surrogate pairs before returning.

Core is pinned in `dependencies.lock.json`; the DA pack hash/format is unchanged.
The legacy pack has no families/ngram evidence. An explicit seed domain catalog is
tested, not bundled/promoted. Choice persistence/adaptive learning are unavailable;
no consent, settings schema, data migration or user-profile writes are added.
See [contract](BridgeContract.md) and [qualification](qualification/WSA-UX-029_slice3.md).

## Settings UI — WSA-UX-029 slices 1–2

`SettingsWindow` uses native WPF ListBox navigation for eight categories and a
separate right-hand ScrollViewer. `SettingsRow`/`SettingsStyles` align grouped
labels, one accessible info action and a value column with system color resources.
`SettingsHelpCoordinator` manages one popup, keyboard dismissal and focus return.
Read-only status/version values retain their actual UI Automation text.

`LegacySettingsWindow` preserves the previous Windows layout and controls. Both
views use `SettingsFormController`, the same view model and settings service.
`SettingsPatch` applies only changed fields against the latest snapshot; unrelated
fields, unknown v1 properties and other-language voice overrides survive. Same-
field conflicts and invalid/future files keep the dialog open without overwriting
data. Atomic replacement/backup stays unchanged. This protects dialogs in the one
app process; it does not introduce a cross-process lock or a new settings schema.

Slice 2 applies ordinary preferences only after successful persistence and replaces
global Save/Cancel with Close. The legacy view keeps explicit drafts/Save/Cancel.
`WindowsAppSettingsService.Changed` applies one saved snapshot to runtime and the
primary view. `_applyingSettings` prevents recursive persistence. `SettingsText`
installs 155 embedded Danish/English dynamic resources, independently of writing
language/packs; the translated scope is Settings/legacy, not all older app copy.

Schema 2 adds the interface preference and separate caret permission at the same
historical file path. V1 loads upgrade in memory; first successful write retains an
exact `.v1.bak`, then the rolling atomic `.bak`. Unsupported/corrupt data refuses
writes and starts with external capture/content collection/caret disabled. Unknown
properties and false preferences survive. [Recovery and evidence](qualification/WSA-UX-029_slice2.md).

`RequestsCaretGeometry` combines permission and explicit placement. MainWindow
skips local/external geometry when false and rejects obsolete in-flight results
using a measurement generation. The overlay hides its follow choice when permission
is off; turning permission on retains static placement. Text/selection reads for
verified commits are separate from geometry. No new linguistic rules are added.

Full account, Hello, analysis and learning controls remain unavailable. Existing bounded metadata/manual
update behavior is preserved; App Installer still owns signed installation and
automatic scheduling. [Slice 1 evidence](qualification/WSA-UX-029_slice1.md).

## Settings qualification boundary — WSA-UX-029 slice 0

The new qualification scripts/register are test tooling, not production Settings
adapters. They fingerprint the actual working-tree inputs and wrap existing gates.
Native host checks distinguish API presence, ordinary-user test access and real
package/Hello-secret authority. Only isolated LocalTest packages can be mutated;
settings restoration is checked even after partial failure. Current Hello is
DeviceNotPresent and both OS update setters return 0x800706F7; neither enables a
product toggle. See [actual evidence and remaining gates](qualification/WSA-UX-029_slice0.md).
The app architecture/framework and shared Core contract are unchanged by slice 0.

## Three boundaries

1. Shared engine/pack boundary: `ISuggestionProvider`, `WordSuggestorCoreCliSuggestionProvider`,
   `LanguagePackValidator` and portable Core SuggestCLI. Core owns ranking,
   language normalization, matching and user-word merging; no C# linguistic engine.
2. Native app/orchestration: `App`, `MainWindowViewModel`, compact `MainWindow`,
   normal `InternalEditorWindow`, non-activating `SuggestionOverlayWindow`, native
   settings/insights/user-word windows. One shared view model and one commit path.
3. Windows adapters: native Notepad text messages, UI Automation/isolated Word COM
   selection reads, screen geometry/DPI, speech/OCR, local stores and MSIX scripts.

The provider is a process boundary, not an in-process Swift ABI. Smoke latency did
not justify replacing it. It now uses bounded versioned stdin/stdout JSON, timeout,
cancellation/process-tree cleanup and dispatcher-side request-generation checks.
No production runtime compiles Swift or searches for sibling source repositories.

## Windows and focus

Toolbar and overlay use WS_EX_NOACTIVATE, WM_MOUSEACTIVATE/MA_NOACTIVATE and
nonfocusable mouse controls. They do not own a document. The editor is a standard,
resizable, activating WPF Window with a named RichTextBox, native file dialogs/menu,
status row and preserved command/legend structure. Settings and user-word dialogs
are normal activating windows. App startup creates no empty document window.

Local mode requires editor activation AND keyboard focus inside its text control.
Opening an editor invalidates external state. Defocus invalidates local pending
suggestions; any external anchor completion must still match the current target.
Own-process windows never become external typing targets. One per-user instance
is enforced; OCR callback helper startup is processed before that gate.

Mouse, Tab and Ctrl+digits all call the view model's candidate acceptance method.
It either performs one guarded local transaction or requests the external adapter;
mouse cannot silently write into the internal editor during an external session.
An acceptance invalidates candidates/pending work so repeated clicks cannot commit
twice. Hotkeys are registered only while external candidates are available.

## Text, caret and placement

All editing offsets are UTF-16. Prefix/selection replacement, suffix spacing and
caret position are computed together in `EditorCommit`; this is editing-boundary
logic, not ranking or lexical normalization. Model text/caret are assigned before
WPF notifications. Real selection changes are distinguished from programmatic
caret updates to avoid collapsing the user's selection. TextPointer mapping uses
the same TextRange representation, including paragraph CRLF and surrogate pairs.
Requests use text BEFORE the caret, not the document end.

Overlay supports ten candidates per page, at most four pages. Follow-caret requires
confirmed geometry; approximate bounds/mouse position are never treated as a caret.
Missing geometry uses a stable static point. Static position is persisted in
physical screen coordinates. `ScreenPlacement` selects the nearest monitor,
converts size for that monitor's DPI, and clamps to its work area, including
negative origins. PerMonitorV2 is declared. Geometry math is tested; real mixed-DPI
hardware qualification is separate from the single-monitor 96-DPI desktop smoke.

## External editing

The first qualified route is Notepad's native Edit/RichEdit text controls. It reads
actual document/caret/selection state instead of logging or reconstructing raw keys.
The snapshot binds top-level HWND, control HWND, process ID/start time, text and
selection. Every commit must still match all of them and the current prefix.
Password/read-only/unsupported controls are rejected before content read.

Commit uses targeted EM_SETSEL/EM_REPLACESEL with bounded SendMessageTimeout, then
verifies resulting text AND caret. It never types into whichever window happens
to be foreground. Clipboard is untouched. Unconfirmed/partial results invalidate
the session and are never automatically retried. EM_GETSEL bounds this route to
60,000 UTF-16 units. Focus changes abort. This route does not claim Word/Outlook/
browser compatibility; they need independently qualified adapters.

UIA/Word COM selected-text import remains available on explicit TXT/TTS actions.
There is no passive full-selection polling. The old generic SendInput commit and
automatic clipboard import were removed for concrete target/focus/privacy defects.
Explicit clipboard import is snapshot-qualified for Notepad, materializes restorable
formats first, verifies focus/selection and clipboard owner/sequence, and preserves
a newer clipboard change instead of overwriting it. Unsupported clipboard formats
abort before replacing the clipboard. Broader routes are a documented migration gate.

## Data and privacy

Existing `%LOCALAPPDATA%\WordSuggestor` is preserved:

| Data | Location/policy |
|---|---|
| Preferences | Historical `settings/settings-v1.json`, schema 3; exact once-only `.v1.bak`/`.v2.bak` migration originals, atomic replace/rolling `.bak`; invalid/future data protected |
| User words | `UserWords/local_lexicon_overlay_v2.json`; Core owns format/normalization |
| Installed packs | `Packs`; old roaming packs stay discoverable; bundled packs remain separate/read-only |
| Insights | `insights/error-insights.jsonl`; kinds/counts by default; word examples require explicit preference |
| Diagnostics | `diagnostics`; stage/result/counts, no raw key/token/document text, titles or process stderr |
| Documents | UTF-8 text at the user-selected file path; buffer retained while editor is hidden; quit/open guards unsaved changes |
| OCR/TTS temporary content | Existing local bridges, cleaned in their lifecycle; no content in diagnostics |
| Credentials | No backend/login implemented and no tokens stored; future credentials must use Windows Credential Manager/DPAPI, never settings/logs |

`WORDSUGGESTOR_DATA_ROOT` is a deliberate test/developer override, not a change to
normal data location. No backend connection, telemetry upload or credential is
needed for local suggestions. Existing historical logs are not silently deleted.
MSIX virtualization declarations keep established user data outside package cleanup;
signed-install/uninstall verification remains a release gate.

## Preserved and deferred

Existing OneCore/SAPI TTS, STT, OCR callbacks, Word selection import, native settings
and insights remain. Their code presence is not proof of audible recognition or
screen-snip behavior. The previous C# suffix-based POS/spelling classifier was
unreliable and duplicated language policy; it is replaced by neutral rendering.
Analysis preferences remain, controls are disabled, and Core document-analysis/
right-click correction integration is the explicit migration plan.

Installer, signing, update trust, rollout/rollback and operator gates are in
[Release.md](Release.md). Current verification and limitations are in
[ManualSmoke.md](ManualSmoke.md) and the dated delivery note.

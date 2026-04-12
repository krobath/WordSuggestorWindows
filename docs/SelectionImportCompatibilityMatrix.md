# Windows Selection Import Compatibility Matrix

Last updated: `2026-04-12`
Sprint: `WSA-TS-002_windows_selection_import_app_compatibility_matrix`
Owner: `Windows track`

## Purpose

Track which Windows applications support WordSuggestor selected-text import through:

- internal editor selection
- Windows UI Automation `TextPattern`
- recent cached UI Automation selection
- guarded clipboard fallback with sentinel detection

This matrix is intentionally empirical. Windows applications vary by control type, process integrity level, browser mode, document protection state, and whether they accept synthetic `Ctrl+C`.

## Diagnostic Signals

The Windows app emits non-content diagnostics to the Visual Studio / debugger output stream and to this local file:

```text
%LOCALAPPDATA%\WordSuggestor\diagnostics\selection-import.log
```

Diagnostics use this prefix:

```text
WordSuggestor selection import:
```

The diagnostics deliberately record only stage, outcome, target handle, and character counts. They must not log the selected text itself.

Expected diagnostic stages:

- `UIA`
- `ClipboardFallback`

Expected outcomes:

- `Skipped`
- `Success`
- `CachedSuccess`
- `NoSelection`
- `Failed`
- `TargetActivated`
- `TargetActivationUnconfirmed`
- `CopyShortcutSent`
- `ClipboardRestored`
- `ClipboardRestoreFailed`

## Result Codes

| Code | Meaning |
|---|---|
| `PASS-UIA` | Import succeeds through Windows UI Automation. |
| `PASS-CB` | Import succeeds through guarded clipboard fallback. |
| `PARTIAL` | Import works only in some controls, modes, or timing windows. |
| `BLOCKED` | Import does not work because the target blocks UIA and fallback copy. |
| `UNTESTED` | Not yet manually tested in this workspace. |

## Priority App Matrix

| Application | Test surface | Expected route | Current result | Notes |
|---|---|---|---|---|
| WordSuggestor internal editor | RichTextBox selection | Internal editor selection | `UNTESTED` | Baseline route; should not use UIA or clipboard fallback. |
| Windows Notepad | Normal text document | UIA or clipboard fallback | `UNTESTED` | Good low-friction control case. |
| Microsoft Word | Editable `.docx` document | UIA or clipboard fallback | `UNTESTED` | Test normal document and protected/read-only document separately. |
| Microsoft Outlook | Message body editor | UIA or clipboard fallback | `UNTESTED` | Test compose window and reading pane separately. |
| Microsoft Edge | Normal HTML text field | UIA or clipboard fallback | `UNTESTED` | Test textarea/input on a simple web page. |
| Google Chrome | Normal HTML text field | UIA or clipboard fallback | `UNTESTED` | Test textarea/input on a simple web page. |
| Google Docs in Edge | Document canvas | Clipboard fallback likely | `UNTESTED` | UIA selection may be limited because the editor is web/canvas-backed. |
| Google Docs in Chrome | Document canvas | Clipboard fallback likely | `UNTESTED` | Test with browser focus already inside the document. |
| Adobe Acrobat Reader | Selectable PDF text | Clipboard fallback likely | `UNTESTED` | PDF viewers often expose inconsistent UIA text selection. |
| Elevated/admin app | Text field in elevated process | Blocked or partial | `UNTESTED` | A non-elevated WordSuggestor process may not be allowed to drive it. |
| Password/secure field | Password input | Blocked | `UNTESTED` | WordSuggestor should not attempt to import sensitive secure-field text. |

## Manual Recording Template

Copy one row per tested app into the matrix above or into the sprint notes:

| Application/version | Test surface | UIA result | Clipboard fallback result | Clipboard restored | Result code | Notes |
|---|---|---|---|---|---|---|
| `<app>` | `<control>` | `<diagnostic>` | `<diagnostic>` | `<yes/no>` | `<code>` | `<observations>` |

## Interpretation

- If UIA reports `Success`, the app is compatible without touching the clipboard fallback path.
- If UIA reports `NoSelection` and clipboard fallback reports `Success`, the app is compatible but depends on synthetic `Ctrl+C`.
- If clipboard fallback reports `TargetActivationUnconfirmed`, repeat the test with the target app focused immediately before clicking `TXT`.
- If clipboard fallback reports `NoSelection`, the target app may have no active selection, may block synthetic copy, or may expose a non-text clipboard format.
- If clipboard fallback reports `ClipboardRestoreFailed`, re-test cautiously because the clipboard owner may be blocking restoration.

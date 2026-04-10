# WordSuggestorWindows Architecture

Last updated: `2026-04-09`
Status: `Draft baseline with overlay and editor-shell parity baselines validated`

## Goal

Deliver a Windows-native WordSuggestor app without destabilizing the existing macOS app.

The Windows port should reuse:

- `WordSuggestorCore` for suggestions, packs, ranking, diagnostics, and learning logic

The Windows port should replace:

- App shell
- Input capture
- Caret and focused-text extraction
- Suggestion overlay window
- Commit-to-target behavior
- Windows packaging and install/update flow

The Windows port should preserve:

- Core information architecture from the macOS app
- Toolbar control ordering and feature entry points
- Suggestion panel semantics (paging, candidate affordances, placement modes)
- Internal editor semantics for analysis coloring and correction surfaces

## Architectural decision

Do not port the current macOS app 1:1.

Instead:

1. Keep `WordSuggestorCore` as the shared engine.
2. Build a Windows-native host application in this repository.
3. Recreate platform behavior through Windows adapters, not AppKit/App Accessibility code reuse.

Windows should look Windows-native, but remain recognizably the same product as the macOS build.
That means:

- native Windows control chrome where it improves clarity,
- the same major surface layout and control ordering as macOS,
- the same shared app icon across platforms,
- Windows-native icons when obvious equivalents exist, with SF Symbols allowed as fallback.

## Why

The current macOS application is heavily tied to:

- `AppKit`
- macOS Accessibility (`AXUIElement`, `AXObserver`)
- macOS event capture (`CGEventTap`)
- `NSPanel`-based floating UI

Those responsibilities are platform-specific and should be treated as adapter layers.

## Target layers

### 1. Shared engine layer

Owned by `WordSuggestorCore`.

Responsibilities:

- Language-pack loading
- Candidate generation
- Context-aware reranking
- Unknown-word learning
- Diagnostics and benchmarks

### 2. Windows app orchestration layer

Owned by `WordSuggestorWindows`.

Responsibilities:

- Application state
- User settings
- Request/response flow between UI and engine bridge
- Feature gating and runtime policy
- coordination across multiple simultaneous UI surfaces

Primary surfaces:

- Floating toolbar shell
- Expandable internal editor surface
- Floating suggestion overlay
- Right-click correction/context popover

### 3. Windows platform adapter layer

Owned by `WordSuggestorWindows`.

Responsibilities:

- Input capture
- Focused-text context
- Caret anchor extraction
- Suggestion commit to target
- Overlay panel placement
- fallback from follow-caret to static placement when caret placement is not reliable

## Adapter surfaces

The Windows implementation should converge on explicit interfaces for:

- `SuggestionProvider`
- `ToolbarShellHost`
- `InternalEditorHost`
- `InputCaptureAdapter`
- `FocusedTextContextAdapter`
- `CaretAnchorAdapter`
- `SuggestionCommitAdapter`
- `SuggestionPanelHost`
- `CorrectionPopoverHost`
- `SelectionImportAdapter`

## Delivery strategy

### Phase 1

Windows visual shell parity baseline.

Use case:

- Launch into a floating top toolbar instead of a normal document window
- Expand into the internal editor from the right-side chevron
- Preserve macOS toolbar ordering and editor structure with Windows-native visuals
- Reuse `WordSuggestorCore` through the existing CLI bridge

### Phase 2

Windows-native suggestion overlay parity.

Use case:

- Show a separate suggestion overlay under the caret in the internal editor
- Support static mode and follow-caret mode
- Support `Ctrl+1` through `Ctrl+0` candidate shortcuts
- Support page navigation and static fallback when caret placement is unavailable or unreliable
- Fall back to static placement when caret placement is unavailable or unreliable

Current state:

- Implemented for the internal editor baseline in `WSA-RT-002`.
- External-app anchoring and commit behavior remain future work.

### Phase 3

Cross-app typing/caret/context integration on Windows.

This phase is highest risk and should only begin after the internal editor path is stable.

Priority external targets:

- mainstream word processors
- email clients
- browsers needed for Google Docs

Policy:

- follow-caret is preferred
- fallback to static placement when caret anchoring cannot be trusted

### Phase 4

Editor correction popover parity.

Use case:

- right-clicking an underlined or flagged word opens the correction/context popover
- popover supports candidate insertion and word-level actions
- hover-triggered popovers can be considered later after right-click parity is stable

## Bridge strategy to WordSuggestorCore

Preferred first implementation:

- Use a thin process bridge around an existing Swift CLI or a dedicated Windows-friendly core host executable

Reason:

- Fastest path to proving end-to-end reuse
- Avoids premature DLL/ABI work
- Keeps early Windows milestones focused on product behavior

Later optimization path:

- Replace process bridge with a more direct host boundary only if process overhead becomes a proven issue

## Non-goals for the first Windows milestones

- Direct port of `GlobalKeyCaptureManager.swift`
- Direct port of `MacSuggestionPanelController.swift`
- Full external-app parity on day one
- Reworking the macOS repository into a shared multiplatform app shell before Windows value is proven

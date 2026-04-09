# WordSuggestorWindows Architecture

Last updated: `2026-04-09`
Status: `Draft baseline`

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

## Architectural decision

Do not port the current macOS app 1:1.

Instead:

1. Keep `WordSuggestorCore` as the shared engine.
2. Build a Windows-native host application in this repository.
3. Recreate platform behavior through Windows adapters, not AppKit/App Accessibility code reuse.

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

### 3. Windows platform adapter layer

Owned by `WordSuggestorWindows`.

Responsibilities:

- Input capture
- Focused-text context
- Caret anchor extraction
- Suggestion commit to target
- Overlay panel placement

## Adapter surfaces

The Windows implementation should converge on explicit interfaces for:

- `SuggestionProvider`
- `InputCaptureAdapter`
- `FocusedTextContextAdapter`
- `CaretAnchorAdapter`
- `SuggestionCommitAdapter`
- `SuggestionPanelHost`
- `SelectionImportAdapter`

## Delivery strategy

### Phase 1

Internal editor only.

Use case:

- Type inside the Windows app
- Get local suggestions from `WordSuggestorCore`
- Accept suggestions in-app

### Phase 2

Windows-native suggestion overlay for the app shell.

### Phase 3

Cross-app typing/caret/context integration on Windows.

This phase is highest risk and should only begin after the internal editor path is stable.

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
- Full feature parity on day one
- Reworking the macOS repository into a shared multiplatform app shell before Windows value is proven

# WordSuggestorWindows

Windows-native host application for WordSuggestor.

This repository is the delivery track for the Windows port of the macOS WordSuggestor app. The shared suggestion engine remains in `WordSuggestorCore`; this repository owns the Windows app shell, platform adapters, packaging, and Windows-specific validation.

## Initial scope

- Native Windows app shell
- Internal editor with local suggestion flow
- Bridge to `WordSuggestorCore`
- Windows-specific input/caret/panel/commit adapters
- Windows packaging and smoke validation

## Documents

- `docs/Plan.md` - active Windows delivery plan
- `docs/Architecture.md` - target architecture and adapter model
- `docs/ParityMatrix.md` - macOS-to-Windows feature parity tracking
- `docs/changes/` - per-sprint implementation notes
- `scripts/build_app.ps1` - reproducible local WPF build on this Windows workspace
- `scripts/test_core_cli.ps1` - local bridge diagnostic for `WordSuggestorCore`

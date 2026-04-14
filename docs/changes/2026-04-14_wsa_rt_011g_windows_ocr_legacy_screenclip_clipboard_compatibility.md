# WSA-RT-011G_windows_ocr_legacy_screenclip_clipboard_compatibility

Date: `2026-04-14`
Status: `Done`

## Why

Recent OCR tests on the active Windows machine showed that the modern `ms-screenclip://capture/image?...&redirect-uri=...` path could launch Snipping Tool without producing the expected cross-hair overlay or callback. From the users perspective, WordSuggestor hid itself and then appeared to do nothing. The older `ms-screenclip:` overlay contract remains useful as a compatibility path because it starts an interactive snip and places the captured image on the clipboard.

## Implemented

- Reworked `WindowsOcrService` so OCR now prefers a legacy `ms-screenclip:` rectangle snip first.
- Added clipboard snapshot + sentinel handling before OCR capture starts.
- Added a wait loop that watches for a real clipboard image after the legacy screenclip overlay launches.
- Persisted the clipboard image to a temp PNG and ran the existing OCR bridge on that file.
- Restored the original clipboard snapshot when the legacy path times out, is cancelled, or fails before OCR succeeds.
- Kept the modern callback/token path as a fallback when the legacy launch itself is unavailable.
- Added token-safe OCR diagnostics for the legacy launch, clipboard-image wait, temp PNG persistence, and clipboard restoration.

## Validation

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1`

## Notes

- This change intentionally prioritizes the user-visible overlay/cross-hair experience on the current machine over strict preference for the modern redirect callback contract.
- Interactive GUI validation is still required because the cross-hair overlay and actual screen snip cannot be verified from a non-interactive build run.

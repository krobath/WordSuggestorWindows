# WSA-UX-029: approved closing slices 12-15

Date: 2026-11-13. User authorizes implementation of all four slices, each documented
and committed in every changed repo before the next slice. No push/publication.

| Slice | Fixed scope | Acceptance boundary |
|---|---|---|
| 12 | Verified typing -> Core boundary -> consent-controlled local import | Native editor and Notepad, cancellation, throttle, old consent never enables capture |
| 13 | Windows unknown-word sharing client and existing Core handoff format | Backend implementation belongs to macOS agent by explicit user decision; no real uploads until deployed contract is approved |
| 14 | Installed updater / signed installation and N-to-N+1 qualification | No unsafe trust or production-policy bypass; production signing and any required external trust action stay explicit |
| 15 | Reproducible final regression, accessibility/DPI and acceptance record | Report actual hardware/manual/native results; never convert blocked or untested items into PASS |

Windows Hello remains user-deferred throughout. macOS app/shared Docs/backend are
read-only here. Shared Core changes must remain additive with old-client tests;
macOS-native execution is not attested by Windows tests. Existing MSIX/App Installer
and O-052 upload contracts remain authoritative. A code-delivered slice can retain
external acceptance blockers; sprint_complete/release_qualified stay false until
all actual acceptance requirements are met.

Next for users: automatic lookup during writing, then separately consented sharing
when the macOS-owned backend is ready, then verified update/install behavior and
final human acceptance. No automatic ranking changes, public upload or official
beta publication follow merely from implementation or software tests.

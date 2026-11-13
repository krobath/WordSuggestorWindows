# WSA-UX-029: approved closing slices 12-15

## Authorized native follow-up (2026-11-13)

The first approved temporary-trust run passed trusted signing and ordinary-user
clean installation, and the installed Core returned a positive identity/version-
bound result. The harness rejected the activation/result handoff; its receipt check
is corrected and passes local gates. The second UAC was canceled. Installed
N+1/rollback/policy acceptance is still open; cleanup of package/trust is verified.
See [exact results and next action](../qualification/WSA-UX-029_slice14_native_followup.md).
Earlier pending-approval/no-install statements are historical preparation results.


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

## Delivery checkpoints

- Slice 12: committed Windows 9966143 / Core ebd2424; physical editor/Notepad PASS.
- Slice 13: committed Windows 888058b / Core 0278cbc; Windows client ready,
  real receiver/activation pending macOS backend owner. No backend changes here.
- Slice 14: Windows b0d0eec committed; signed but untrusted series, no
  installed/native acceptance without explicit trust approval. See slice14 evidence.
- Slice 15: independent final source/desktop regression and UIA name correction;
  see [final evidence](../qualification/WSA-UX-029_slice15.md) and
  [remaining operator/human acceptance](../FinalAcceptance.md).

These checkpoints do not close sprint_complete or release_qualified. Missing
hardware/receiver/signing/installed evidence remains an acceptance dependency.

Final slice 15 software result: PASS all 14 clean-source sequential gates,
including 16 Settings suites and native editor/Notepad GUI. Native installed /
human / real-backend acceptance remains explicitly open. The initial transient
GUI timeout and unchanged retry are retained in the qualification record.

# WSA-UX-029 slice 11 — explicit unknown-word import

Date: 2026-11-13. Parent: WSA-UX-029_windows_settings_sidebar_and_functional_parity.

## Deliverable

Users explicitly choose one Danish word, approve online lookup and import validated
dictionary metadata into a separate local Core-format overlay. Retain WPF and the
existing Core service; no passive capture, sharing, backend changes or document
insertion. A06 becomes partial, A07 unavailable; full sprint/release stays open.

## Acceptance

- Native accessible dialog, settings consent default-No and persisted schema 7.
- Real Core dictionary lookup and exact-match/metadata selection; no C# language rules.
- Two-phase read-only lookup/prepare with final Windows atomic persistence guard.
- Cancellation/revocation/stale context cannot create a late learned snapshot.
- Corrupt/future main and backup files preserved; manual words and pack unchanged.
- Imported words contribute offline after restart and can be cleared explicitly.
- Additive Core API and deliberate shared provider bug fixes documented/tested.
- Existing gates plus import state/desktop gates run, limitations stated explicitly.
- Repo-local notes/status/index updated; one commit in each changed repo, no push.

See [contract](../UnknownWordImport.md) and [evidence](../qualification/WSA-UX-029_slice11.md).
Next build consent-controlled learning while writing from verified completed-word
events, with focus and document tests. This slice enables only explicit import;
automatic observation, sharing and deferred native release gates remain off/open.

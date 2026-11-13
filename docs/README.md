# WordSuggestorWindows — dokumentationsindeks

## Authorized native follow-up (2026-11-13)

The first approved temporary-trust run passed trusted signing and ordinary-user
clean installation, and the installed Core returned a positive identity/version-
bound result. The harness rejected the activation/result handoff; its receipt check
is corrected and passes local gates. The second UAC was canceled. Installed
N+1/rollback/policy acceptance is still open; cleanup of package/trust is verified.
See [exact results and next action](qualification/WSA-UX-029_slice14_native_followup.md).
Earlier pending-approval/no-install statements are historical preparation results.


## WSA-UX-029 slice 15 - final software and acceptance boundary (2026-11-13)

Settings row editors now expose localized UIA names, including after language
changes. Layout automation covers two account states, two locales and all eight
categories. The existing clean-source script supports FullRegression and records
software gates separately from native/human release acceptance. See the
[procedure and remaining matrix](FinalAcceptance.md) and
[measured results](qualification/WSA-UX-029_slice15.md).
Sharing receiver, native installed updates, human accessibility/mixed DPI and
user-deferred Hello remain explicit acceptance gaps; full sprint/release stay open.


## WSA-UX-029 slice 14 - signed local fixtures, native acceptance pending (2026-11-13)

The existing MSIX flow now prepares three isolated signed test packages without
installing or changing device trust. The update harness covers extended data,
higher-version rollback and packaged policy qualification. Build, 56 release
checks, 54 standalone payload GUI checks and synthetic Core/store readback pass.
Installed update/rollback and G05 remain unqualified pending explicit trust
approval; no production signer/feed/publication exists. See [actual evidence](qualification/WSA-UX-029_slice14.md)
and [release procedure](Release.md). macOS/backend/shared sources unchanged.


## WSA-UX-029 slice 13 - sharing client ready, receiver pending (2026-11-13)

The Windows client prepares the unchanged Core handoff, persists an account-bound
idempotent queue and reuses secure account HTTPS transport. Schema 9 adds separate
profile-bound consent; all real sharing stays source-gated/off. The user assigned
the receiver to the macOS backend agent. See [client contract](UnknownWordSharing.md),
[backend prompt](handoffs/WSA-UX-029_slice13_backend_agent_prompt.md) and
[qualification](qualification/WSA-UX-029_slice13.md). A07 remains unavailable;
mock success is not deployed-backend acceptance. Earlier entries are historical.


## WSA-UX-029 slice 12 - verified typing lookup (2026-11-13)

Automatic Danish lookup now works in the native editor and supported Notepad with
separate opt-in, Core-owned word boundaries, rate limits and late-write protection.
A06 is implemented; A07 remains unavailable pending the macOS-owned backend. Schema
8 never promotes manual consent into automatic permission. See [contract](AutomaticUnknownWords.md),
[qualification](qualification/WSA-UX-029_slice12.md) and the approved
[remaining four-slice scope](sprints/WSA-UX-029_closeout_slices12_15.md).
Earlier dated entries below are historical. Hello remains user-deferred; sprint
and release acceptance remain open. No macOS app/library or backend files changed.


## WSA-UX-029 slice 11 — explicit unknown-word import, 2026-11-13

Users can explicitly look up and import one Danish word through Settings > Advanced
or the editor menu, after separate online consent. Core validates dictionary data;
Windows atomically saves the separate local overlay. Imported words work offline
and survive restart; opting out stops lookup, explicit clear removes imported data.
A06 is partial (explicit import only), A07 unavailable; automatic typing capture is
off. Schema 7 defaults new/migrated consent to off. See [contract](UnknownWordImport.md),
[plan](sprints/WSA-UX-029_slice11_explicit_unknown_word_import.md) and
[qualification](qualification/WSA-UX-029_slice11.md). Older dated statements below
are historical. Native Hello remains user-deferred and release gates remain open.


## WSA-UX-029 slice 10 — read-only completed-word boundary, 2026-11-12

The Windows adapter can explicitly ask Core whether one completed token would
emit its existing unknown-word observer signal. This foundation is implemented;
it does not enable A06/A07, capture typing, store words or perform online lookup.
See [the contract](CommittedWordAssessment.md), [slice plan](sprints/WSA-UX-029_slice10_committed_word_assessment.md)
and [qualification](qualification/WSA-UX-029_slice10.md). Core changes only its CLI,
tests and local docs; library/pack/macOS source remain unchanged.

Next implement a reviewed Core-owned provider/store and explicit lookup consent
before users can enable local unknown-word learning. Public sharing, Hello native
acceptance (user-deferred), installed update policy and official beta remain gated.
Earlier dated next-step text below is historical.


## Beta tooling cleanup — 2026-11-12

WSA-RT-017 records the existing Windows x64 MSIX/App Installer and backend O-052
uploader as one reviewable delivery. See the [release runbook](Release.md),
[qualification evidence](qualification/WSA-RT-017_release_cleanup.md) and
[change note](changes/2026-11-12_wsa_rt_017_beta_publication_tooling.md).
Production signing, trusted install/update and live SSH/publication remain open.
No release is published and Windows Hello acceptance stays user-deferred.


## Editor tabs closeout — 2026-11-12

WSA-UX-013 retains WPF/Core and adds independent native document tabs with guarded
save/close and local draft recovery. See the [change note](changes/2026-11-09_wsa_ux_013_editor_tabs.md)
and [software/desktop evidence](qualification/WSA-UX-013_editor_tabs.md).
Human accessibility/DPI, speech-switching and signed-update draft retention remain
open. Windows changes do not modify the macOS app, Core or backend.


## Developer restart closeout — 2026-11-12

WSA-DX-005 is implemented and qualified with 14 owned-fixture checks. Builds fail
early on a locked executable; `run_app.ps1 -Restart` requests save-aware shutdown
without force termination. Relative paths follow the PowerShell working directory
and launch-only text/environment are contained. See the [change note](changes/2026-11-09_wsa_dx_005_safe_developer_restart.md).


## Latest: slice 9 Advanced diagnostics candidate — 2026-11-12

- [Categories, storage, privacy, migration and Core summary](Diagnostics.md).
- [Exact-source evidence and manual/native matrix](qualification/WSA-UX-029_slice9.md).
- [All 72 current control statuses](qualification/settings-parity-status.v1.json).
- [Change note and exact changed files](changes/2026-11-12_wsa_ux_029_slice9_advanced_diagnostics.md).

Five diagnostic controls are effective; the same unknown-word controls are visible
but unavailable. They neither send words nor pretend manual user-word insertion is
learning. WPF/Core/pack architecture is retained. The original slice-0 register is
an immutable baseline; the new status matrix records current implementation and
separate evidence boundaries. No blanket 72-control/release signoff is claimed.
Next acceptance work runs remaining human accessibility/DPI and trusted installed
update tests, while a Core-owned unknown-word contract is prepared. Hello remains
user-deferred. All seven isolated-source software gates pass, including actual editor/Notepad
and Media reruns in the active session. Native installed update, human acceptance
and unavailable unknown-word features are still open. Earlier dated status entries
below are historical.

## Latest: slice 8 update implementation candidate — 2026-11-12

- [Actual OS policy, manual installer handoff, safety and native gate](WindowsUpdates.md).
- [Slice 8 software results and manual/native matrix](qualification/WSA-UX-029_slice8.md).
- [Change note and exact file scope](changes/2026-11-12_wsa_ux_029_slice8_app_installer_updates.md).

Manual update orchestration and actual-state UI are implemented. Fresh isolated
state/contract gates pass; desktop acceptance is blocked by a disconnected session
and Media failed on a black fixture also reproduced with the prior baseline.
The full suite is not green. In-app automatic
policy editing is still read-only because trusted packaged change/read/restore is
not qualified. Existing OS scheduling is unaffected. Hello hardware acceptance is
deferred by the user (2026-11-12); no further native Hello gate was executed.
Next work builds Advanced diagnostics controls, then closes the outstanding real
installation/update and human accessibility tests before public release.
Older dated entries below record earlier checkpoints.

## Latest: slice 7 account implementation candidate — 2026-11-11

- [Account, backend wire, credentials, Hello and recovery](AccountAndHello.md).
- [Slice 7 evidence and manual/native boundaries](qualification/WSA-UX-029_slice7.md).
- [Change note and changed files](changes/2026-11-11_wsa_ux_029_slice7_account_hello.md).

Account controls are implemented. Native PIN/biometric positive qualification is
blocked by this host's unavailable key service; synthetic key tests do not close
that gate. Existing backend/Core/macOS remain unchanged. Next engineering slice
is actual App Installer update authority, with Hello/live-account acceptance still
open. Earlier dated statuses below record their respective checkpoints.

## Latest: slice 6 analysis and local choices — 2026-11-11

- [Analysis, choice storage, privacy and recovery](AnalysisAndLearning.md).
- [Slice 6 test evidence and native/manual matrix](qualification/WSA-UX-029_slice6.md).
- [Change note and file scope](changes/2026-11-11_wsa_ux_029_slice6_analysis_learning.md).

The desktop is now connected; new native tests supersede the earlier locked-
session block for the tested flows. Human Narrator/audio and extra DPI/monitors
remain unqualified. Profile/Hello is the next feature slice; no backend learning.
Earlier dated slice descriptions below are historical evidence.

## Historical: slice 5 implementation candidate, 2026-11-11

- [Suggestion runtime/data contract](Suggestions.md).
- [Slice 5 evidence and outstanding desktop tests](qualification/WSA-UX-029_slice5.md).
- [Change note and exact file scope](changes/2026-11-11_wsa_ux_029_slice5_suggestion_settings.md).

At that candidate checkpoint, native qualification was blocked by a locked
desktop. See the current slice-6 evidence above for the tested replacement source.
Earlier dated slice statuses below remain historical evidence only.

Opdateret 2026-11-10. Dette er den lokale dokumentation for **Windows**.
macOS-dokumentation ejes af `WordSuggestor/docs/`; fælles produkt-/kontraktoversigter
og platformindgange ejes af `WordSuggestorDocs/`. Implementeringsstatus her må ikke
aflæses som status for andre platforme.

## Aktuelt arbejde

- [WSA-UX-029: Settings-sprint](sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md):
  oprettet 2026-11-10 efter alle produktvalg; ti slices og komplet kontrol-/testmapping.
  **Slice 0-baseline og slice 1–4 leveret; fuld funktionsparitet åben.**
- [Slice 4: oplæsning, strategi og markering](qualification/WSA-UX-029_slice4.md):
  schema 3, native tempo/timing og editor/Notepad; [talekontrakt/gendannelse](Speech.md)
  og [ændringsnote](changes/2026-11-10_wsa_ux_029_slice4_speech.md).
- [Slice 3: Core-capabilities og typed adapter](qualification/WSA-UX-029_slice3.md):
  versionsforhandling, kildevalg, domæner, ordformer og UTF-16-analyse; UI-adoption
  og lokal læring følger senere. [Ændringsnote](changes/2026-11-10_wsa_ux_029_slice3_core_settings_adapter.md).
- [Slice 2: løbende gemning, sprog og caret-tilladelse](qualification/WSA-UX-029_slice2.md):
  schema 2, backup/gendannelse, dansk/engelsk Settings og testbevis.
  [Ændringsnote](changes/2026-11-10_wsa_ux_029_slice2_live_settings.md).
- [Slice 1: sidebar, hjælp og Windows-legacy](qualification/WSA-UX-029_slice1.md):
  faktisk testomfang, manuelle restgates og kildeafgrænsning.
  [Ændringsnote](changes/2026-11-10_wsa_ux_029_slice1_settings_sidebar.md).
- [Slice 0: faktisk kvalifikation og begrænsninger](qualification/WSA-UX-029_slice0.md):
  ti baseline-gates PASS; Hello DeviceNotPresent og OS-update 0x800706F7 er åbne
  positive integrationsgates. [Maskinlæsbar evidens](qualification/WSA-UX-029_slice0_evidence.json)
  og [72 kontrolposter](qualification/settings-controls.v1.json) er Windows-ejede.
- [Settings: reference- og funktionsanalyse](analysis/2026-11-09_settings_parity_analysis.md):
  alle 11 billeder, 72 kontrol-/funktionsposter, macOS-kildeadfærd og Windows-mangler.
  Kildereference og de tre produktvalg er afklaret.
- [Dokumentation og ejerskab på tværs af platforme](DocumentationOrganization.md):
  lokale implementeringskilder, fælles kontrakter, parity og konsolidering.
- [Plan](Plan.md): Windows-sporets arbejde og kvalifikation.
- [Parity-matrix](ParityMatrix.md): implementering, evidens og kendte forskelle.

## Arkitektur og udvikling

- [Repository-audit](ReleaseAudit.md)
- [Arkitektur](Architecture.md)
- [Core-/packgrænse](BridgeContract.md)
- [UI-paritet](UiParityPlan.md)
- [Byg, test og start](../README.md)

## Test og udgivelse

- [Manuel og automatiseret smoke](ManualSmoke.md)
- [Selection-import og applikationskompatibilitet](SelectionImportCompatibilityMatrix.md)
- [Release, signing og opdatering](Release.md)
- [Ændringsnoter](changes/): dato, faktisk leverance, testomfang og begrænsninger.

## Lokale dokumentationsregler

Nye analyser placeres i `analysis/`. Afklarede fremtidige sprints placeres i
`sprints/` og registreres i Plan og dette indeks. Nye ændringsnoter placeres i
`changes/`; de skal skelne tydeligt mellem analyse, implementeret og verificeret.
Bevar eksisterende historik og dokumentplaceringer, og opdatér relevante links.
Fælles konsolidering er en særskilt opgave; Windows-agenten vedligeholder ikke en
parallel kopi af Mac- eller backend-implementationen.

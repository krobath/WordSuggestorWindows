# WordSuggestorWindows — dokumentationsindeks

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

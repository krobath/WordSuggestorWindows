# WordSuggestorWindows — dokumentationsindeks

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

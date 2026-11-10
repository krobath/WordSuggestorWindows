# WordSuggestorWindows — dokumentationsindeks

Opdateret 2026-11-10. Dette er den lokale dokumentation for **Windows**.
macOS-dokumentation ejes af `WordSuggestor/docs/`; fælles produkt-/kontraktoversigter
og platformindgange ejes af `WordSuggestorDocs/`. Implementeringsstatus her må ikke
aflæses som status for andre platforme.

## Aktuelt arbejde

- [WSA-UX-029: Settings-sprint](sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md):
  oprettet 2026-11-10 efter alle produktvalg; ti slices og komplet kontrol-/testmapping.
  **Slice 0-baseline og slice 1-Settings-shell leveret; fuld funktionsparitet åben.**
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

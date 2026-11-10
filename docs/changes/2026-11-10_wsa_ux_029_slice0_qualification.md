# WSA-UX-029 slice 0 — kvalifikationskode og baseline

Dato: 2026-11-10. Ejer/platform: Windows. Status: værktøjer og baseline leveret;
positiv Hello-/updatergodkendelse er åben. [Evidensrapport](../qualification/WSA-UX-029_slice0.md).

## Summary

Implementeret maskinlæsbart 72-posters kontrolregister, working-tree-/inputfingerprints,
native host-/OS-prober, isolated-package safetygrænse og samlet runner over de
eksisterende gates. Frisk kildeeksport og baseline passerer. Reelle miljøbegrænsninger
er registreret som blokeret/ikke kørt, ikke som succes eller fungerende produktkontrol.

## User-visible behavior

Ingen ændring af produktets Settings-vindue, defaults, samtykke eller konto. Slice 1
skal bygge venstremenu/legacyvisning; Hello og styrbar OS-update kræver fortsat
positive beviser. Dette er ikke et fuldt færdigt Settings-sprint eller en public RC.

## Implementation details

- `settings_qualification.ps1`: begrænsede WinRT-capabilitykald, type/HRESULT-
  diagnostik, exact test identity/publisher, ordinary-user-krav, safe restoration,
  paused-/unknown-state-afvisning og native/cmdlet-resultater.
- `test_settings_qualification.ps1`: 35 reelle process-/kontrakt-/mock-boundarychecks,
  herunder no-production-mutation, partial failure, strict booleans og exitkoder.
- `test_settings_platform.ps1`: read-only som standard; ændring kun med særskilt
  LocalTest-family. API-tilstedeværelse er ikke produktgodkendelse.
- `test_settings_slice0.ps1`: eksisterende scripts, bounded WinRT-waits,
  kilde-/packbinding, immutable runmappe, per-gate exit/tid/log og app-/testdigest
  før/efter. Stderr behandles som evidens; child exit bestemmer succes.
- Registret har labels/ID/slice/reference og vedtagne Windows-politikker, men ingen
  screenshotværdier, hemmeligheder eller automatisk afledt brugersamtykke.

## Files changed

- `scripts/settings_qualification.ps1` (ny)
- `scripts/test_settings_qualification.ps1` (ny)
- `scripts/test_settings_platform.ps1` (ny)
- `scripts/test_settings_slice0.ps1` (ny)
- `docs/qualification/settings-controls.v1.json` (ny)
- `docs/qualification/WSA-UX-029_slice0.md` (ny)
- `docs/qualification/WSA-UX-029_slice0_evidence.json` (ny, sanitiseret)
- `README.md`
- `docs/README.md`
- `docs/Plan.md`
- `docs/Architecture.md`
- `docs/ParityMatrix.md`
- `docs/UiParityPlan.md`
- `docs/ManualSmoke.md`
- `docs/DocumentationOrganization.md`
- `docs/analysis/2026-11-09_settings_parity_analysis.md`
- `docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `docs/changes/2026-11-09_settings_parity_analysis.md`
- `docs/changes/2026-11-10_wsa_ux_029_settings_sprint_created.md`
- `docs/changes/2026-11-10_wsa_ux_029_slice0_qualification.md` (denne note)

Analyse-/planlægningsfilerne er med som WSA-UX-029's hidtil uncommitted grundlag.
Tracked docs stages kun for Settings/slice-0-hunks; tidligere editor-/releasehunks
bevares i arbejdsfilen og kommer ikke med ved et bredt git add.

## Feature flags / settings impact

Ingen nye produktfeatures aktiveret. Updaterforsøg anvendte kun en særskilt lokal
test-MSIX; flag og øvrig konfiguration var uændret efter fejl. Ingen ændring af
Windows-minimum, officiel package identity, public feed eller de vedtagne valg.

## Logging / telemetry impact

Ingen produkttelemetri. Ignored artifacts har testlogs og relative sourcehashes;
den committed evidens har ingen absolutte maskinstier, bruger-/kontonavne,
credentials, rå dokumenttekst, nøgler eller producerede installationsfiler.

## Validation performed

Samlet runner med `-Desktop -CleanSource`: ti gates PASS. Delresultater: 35 nye
kvalifikationschecks, app 92, Core seks, desktop 42, media tre, launch ni og release
43. Frisk kildeeksport genbyggede SQLite/Core/app og passerede seks/92 checks.
App: nul warnings/errors; eksisterende Core pkg-config/Sendable-advarsler består.

Native Hello: DeviceNotPresent og KeyCredential false. User-verification og
beskyttet-secret-roundtrip **ikke kørt**. Native/cmdlet OS-update på build 26300
med lokal file-feed-fixture: **0x800706F7**, verificeret uændret. Begge negative
resultater er bevaret i evidensen. Testpakke, privat testnøgle og midlertidig
ikke-CA leaf-tillid er fjernet, og fravær er kontrolleret. Ingen offentlig upload.

Første testfixture blev afvist før trust var tilgængelig; den nøgle blev ryddet op.
Første samlede runner blev afbrudt af PowerShell-stderr-behandling. Rækkefølgen og
runneren blev rettet, sidstnævnte regressionstestet, og slutrun passerede uden at
svække eksisterende testforventninger. Se rapporten for præcis bevisgrænse.

## Known limitations / follow-up

Positiv Hello-protection og OS-updateændring er ikke kvalificeret. Kræver passende
provisioneret testmiljø, konkret sikker adapter og relevante Windows-/feed-/package-
kontekster. Manglende ekstern/hardwareevidens må ikke skjules som en green toggle.
Slice 1 kan implementere navigation/legacy uden at aktivere disse funktioner.

Baseline gælder den registrerede arbejdsmappe med tidligere editor-/releaseændringer,
ikke ren HEAD. De tidligere app-/releasekodeændringer bevares uden for dette commit.
Kun Windows-repoet ændres/committes; Mac, Core, backend og fælles docs ændres ikke.
Der pushes eller publiceres ikke.

## Consolidation notes for WordSuggestorDocs

Senere separat konsolidering kan linke til lokal slice-0-evidens og registrere
værktøjs-/baselineleverance med åbne positive platformgates. Kopiér ikke rå artifacts
eller implementeringsplaner til fælles/Mac-docs. Ingen påstand om færdig Settings-
paritet, Hello-login eller fungerende bruger-toggle til OS-opdatering.

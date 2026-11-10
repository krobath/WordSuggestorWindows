# WSA-UX-029 — Windows Settings-sprint oprettet

Dato: 2026-11-10. Type: afklaring og planlægning. Platform/ejer: Windows.

Efterfølgende samme dag: [slice 0-værktøjer og baseline er leveret](2026-11-10_wsa_ux_029_slice0_qualification.md).
Den oprindelige planlægningsstatus nedenfor er historik, ikke aktuel fuld sprintstatus.

## Summary

Oprettet [WSA-UX-029_windows_settings_sidebar_and_functional_parity](../sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md)
efter brugerens svar på alle tre produktspørgsmål. Den lokale analyse er opdateret,
og Windows-indeks, plan, UI-plan og parity-status linker til sprintet.

## User-visible behavior

Ingen appændring i denne leverance. Sprintet er planlagt, ikke implementeret.
Ved implementation er målet native venstremenu og samme kontrolsemantik som Mac,
med Windows Hello PIN/biometri, Ctrl/Ctrl+Shift/Alt lane-defaults og den hidtidige
Windows-settingsvisning som legacy under migrationen.

## Implementation details

Mac ba18b451372dd2e3f125e557c5db45894f4e1ae3 er kanonisk reference. Lokal HEAD,
origin/main og GitHubs refs/heads/main er verificeret til denne hash; ingen
switch/merge eller yderligere Mac-push var nødvendig. Det indledende sandbox-
netværksopslag fejlede; samme read-only ls-remote lykkedes uden for sandboxen.

Sprint-ID 029 havde ingen forekomst i de kendte Windows-/Mac-/fælles docs før
oprettelsen. Planen indeholder ti slices (0–9), alle 72 kontrolposter, klare
Core-/backend-/Windows-grænser, teknisk tidlig kvalifikation af Hello/updater,
data-/legacy-migration og automatiske/manuelle acceptgates. Teknisk utilgængelige
funktioner tæller ikke som fuld paritet ved blot at vise deres kontroller.

## Files changed

- `docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md` (ny)
- `docs/analysis/2026-11-09_settings_parity_analysis.md`
- `docs/README.md`
- `docs/Plan.md`
- `docs/ParityMatrix.md`
- `docs/UiParityPlan.md`
- `docs/DocumentationOrganization.md`
- `README.md`
- `docs/changes/2026-11-09_settings_parity_analysis.md` (dateret henvisning; historikken bevaret)
- `docs/changes/2026-11-10_wsa_ux_029_settings_sprint_created.md` (denne note)

## Feature flags / settings impact

Ingen runtime-, settings-, default- eller samtykkeændring. Kun produktvalg og
fremtidige acceptkriterier er registreret. Ingen credentials eller screenshots
med persondata tilføjet.

## Logging / telemetry impact

Ingen. Ingen login, backendmutationer, installation eller OS-/Hello-indstillinger
ændret som del af planlægningen.

## Validation performed

- Git-status undersøgt før ændringer; eksisterende kode-/releasearbejde bevaret.
- Mac remote, lokal HEAD/origin/main og GitHubs main-reference verificeret.
- Sprint-ID kontrolleret i kendte planer før oprettelse.
- PASS: 66 lokale links, alle 72 registerposter dækket præcis én gang af den
  primære slice-mapping, ti planlagte slices og tre lukkede produktvalg.
- PASS: whitespace-kontrol af alle ti berørte dokumenter og `git diff --check`.
- Ingen builds eller nye funktionstests kørt: appkode er uændret, og sprintet er
  ikke startet. Tidligere PASS-resultater er ikke genbrugt som sprint-evidens.

## Known limitations / follow-up

Planens tekniske dependencies er ikke implementeret: Core-grænse, konto/Hello,
analyse/læring og faktisk OS-updatekontrol kræver deres egne resultater. Signering,
testkonto og fysisk Hello-/DPI-hardware kan blive nødvendige til kvalifikation;
ingen af disse adgange påstås tilgængelige ved planoprettelsen.

Næste konkrete arbejde er slice 0: baseline og platformgrænser. Dernæst bygges den
venstre Settings-navigation og adskilt Windows-legacyvisning. Ingen ny kontrol er
slået til i den nuværende app. Ingen commit/push/publicering; ikke autoriseret.

## Consolidation notes for WordSuggestorDocs

Ved senere separat docs-konsolidering: link Windows-platformindgangen til sprintet
og registrér status som planlagt med afklarede produktvalg. Mac-reference ba18b45
forbliver read-only. Kopiér ikke sprintets implementeringsplan ind i Mac- eller
fælles platformdokumentation; Windows ejer sin egen status og testmatrix.

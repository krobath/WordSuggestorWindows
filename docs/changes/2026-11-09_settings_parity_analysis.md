# Windows Settings — analyse og dokumentationsejerskab

Dato: 2026-11-09. Type: analyse/dokumentation. Intet implementationssprint er
oprettet endnu; tre produktvalg afventer brugerens svar.

Historisk status ovenfor gælder analysedagen. Den 2026-11-10 blev alle valg
afklaret, og [WSA-UX-029 blev oprettet](../sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md).
Se [den nye ændringsnote](2026-11-10_wsa_ux_029_settings_sprint_created.md).

## Summary

Gennemgået brugerens 11 macOS-skærmbilleder og den tilgængelige macOS-kilde,
inklusive kontrolbindinger, runtimeforbrugere, data-/samtykkepolitik og conditional
kontoflows. Sammenholdt med Windows' faktiske settingsmodel, UI, TTS, updater og
Core JSON-bro. Dokumenteret anbefalet ejerskab for flere platforme.

## User-visible behavior

Ingen ændring. Settings-vinduet er endnu ikke ombygget. Brugeren har fået spørgsmål
om Windows Hello, lane-genveje og legacy-dialogens scope. Den tidligere forespørgsel
om nyere Mac-kilde er afklaret med reference `ba18b45`.

## Implementation details

Dokumenteret venstrenavigation og hele kontrolregisteret, live settings vs. eksplicit
profilgemning, faktiske Windows-mangler, Core-/sikkerheds-/updateafhængigheder,
persistens/migration og testkrav. Læring er hverken automatisk ranking eller implicit
upload. UI-synlighed/gemte præferencer er ikke sidestillet med fungerende runtime.

Windows-implementationens dokumentation er lokal i dette repo. Mac-/Core-/backend-
repos er referencekilder, og WordSuggestorDocs er et senere fælles konsolideringslag.
Eksisterende dokumenter og brugerens tidligere kode-/releaseændringer er bevaret.

## Files changed

- `docs/analysis/2026-11-09_settings_parity_analysis.md` (ny)
- `docs/DocumentationOrganization.md` (ny)
- `docs/README.md` (ny)
- `README.md` (indekslinks)
- `docs/Plan.md` (analysefase; intet nyt sprint)
- `docs/ParityMatrix.md` (ny særskilt settings-paritetsrække)
- `docs/changes/2026-11-09_settings_parity_analysis.md` (denne note)

## Feature flags / settings impact

Ingen ændring af settings, defaults, samtykke eller brugerdata. Billedernes værdier
er ikke importeret. Ingen nye UI-kontroller aktiveret.

## Logging / telemetry impact

Ingen. Ingen login-/backendhandlinger eller secret-læsning. Referencebilleder med
kontodata kopieres ikke ind i repositoryet.

## Validation performed

- Git-status undersøgt før ændringer; eksisterende Windows-arbejde bevares.
- Statiske kilde-/forbrugeropslag i Mac og Windows; kildecommit angivet i analysen.
- Sammenlignet alle 11 brugerleverede billeder med kategorier/kontroller.
- Officielle Microsoft-referencer læst for Hello og App Installer-grænsen.
- PASS: 31 lokale dokumentlinks, 72 unikke kontrolposter, alle 11 screenshotreferencer
  og tre eksplicit åbne produktvalg valideret.
- PASS: `git diff --check`. Mac-, Core- og fælles docs-arbejdsmapper er fortsat rene.
- Ingen nye builds/GUI-/macOS-/auth-/update-integrationstests kørt: appkode er
  uændret i denne opgave, som udtrykkeligt er analyse før sprintoprettelse.

## Known limitations / follow-up

Q1–Q3 afventer. Den præcise hash for den installerede Mac-binær er ikke attesteret;
den kvalificerede kilde og release-noten matcher den viste version og funktionerne.
Windows OS-updaterstyring og sikker sessionbeskyttelse kræver tidlig teknisk
kvalifikation. Fuld semantik kræver Core- og konto-/analysefunktioner ud over layout.

Når produktvalgene er afklaret, oprettes Windows-sprintet med konkrete slices og
kontrol-for-kontrol acceptkriterier. Ingen implementering eller offentlig release
er erklæret færdig. Ingen commit/push; ikke autoriseret.

## Consolidation notes for WordSuggestorDocs

Ved en senere separat konsolidering: link Windows-platformindgangen til det nye
lokale indeks og analysen, og registrér Settings-paritet som analyse/afventer valg.
Eventuelt fælles semantisk settings-register skal adskilles fra platformspecifik
rendering og fra Core-/backendkontrakternes autoritative ejere. Flyt ikke levende
Windows- eller Mac-implementeringsnoter ind i den fælles repo.

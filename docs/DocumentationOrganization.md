# Dokumentation på tværs af platforme

Dato: 2026-11-09; opdateret 2026-11-10 med første lokale sprint. Ejer: Windows-sporet. Dette dokument er Windows-sporets
dokumenterede anbefaling og lokale arbejdsregel. Det flytter eller omskriver ikke
macOS-/Core-/backend-/fælles dokumentation.

## Anbefalet ejerskab

Bevar lokal implementeringsdokumentation i hvert kode-repository og brug
`WordSuggestorDocs` som fælles indeks og konsolideret kontraktlag. Det følger den
eksisterende `90_agent_ops/LOCAL_FIRST_DOCUMENTATION_WORKFLOW.md` og ejerskabskortet,
hvor Windows allerede har sit eget write-scope. At en ældre opremsning ikke nævner
nyere platformrepos ændrer ikke dette ejerskab.

| Indhold | Autoritativ placering | Eksempel |
|---|---|---|
| Windows-implementation | `WordSuggestorWindows/docs/` | WPF-settings, UIA/caret, Credential Manager/Hello, MSIX, Windows-smoke |
| macOS-implementation | `WordSuggestor/docs/` | SwiftUI/AppKit, AX, Keychain/Touch ID og Sparkle |
| Andre native platforme | Det respektive platformsrepo, `docs/` | Linux-/ChromeOS-/iPadOS-UI, adaptere og release |
| Fælles sproglig funktion | `WordSuggestorCore/docs/` | Engine, versionerede adapterkontrakter, lingvistiske fixtures |
| Backend/API | `WordSuggestorBackend/docs/` | Auth, profiler, capabilities, synkronisering og publiceringskontrakt |
| Fælles produktbetydning | Eksisterende fælles kontrakter i `WordSuggestorDocs/` | Hvad en indstilling betyder, privacy og tværgående accept |
| Tværgående overblik | `WordSuggestorDocs/20_platforms/<platform>/` og fælles parity-indeks | Korte status-/ejeroversigter med links til lokale kilder |

Én platformsmappe i den fælles docs-repo er dermed en indgang til platformen,
ikke en anden komplet udgave af dens levende dokumentation. En rettelse til
Windows Settings skal ikke kræve samtidige rettelser til tre kopier af samme tekst.

## Windows-struktur

Det eksisterende rodniveau og historikken bevares. Vi supplerer med en lokal
indgang og mapper, når en dokumentfamilie faktisk opstår:

```text
WordSuggestorWindows/
  README.md                         hurtig build/run/test og dokumentationslinks
  docs/
    README.md                       Windows-indeks, ejerskab og aktuel status
    Architecture.md                 Windows-arkitektur og adaptergrænser
    Plan.md                         Windows-arbejde og sprintstatus
    ParityMatrix.md                  konkret Windows-status og kendte forskelle
    UiParityPlan.md                  Windows-UI og referencedesign
    ManualSmoke.md                  Windows-tests og faktiske observationer
    BridgeContract.md               Windows-klientens brug af Core-kontrakten
    Release.md                      Windows-installation/signering/update
    DocumentationOrganization.md    denne arbejdsregel og anbefaling
    analysis/                       evidens, produktspørgsmål og beslutningsgrundlag
    qualification/                  Windows-kontrolregister og sanitiseret testbevis
    sprints/                        afklarede Windows-sprints og deres acceptplan
    changes/                        daterede, uforanderligt forståelige leverancenoter
```

`analysis/` blev oprettet med Settings-analysen. `sprints/` er oprettet 2026-11-10
med det afklarede [Windows Settings-sprint](sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md). Historiske sprintnoter
flyttes ikke blot for at gøre træet ensartet. Mac-repoets eksisterende
`WSA-UX-026_...` er en reference, ikke Windows-sprintets statusdokument.

## Hvad der bør være fælles

Et tværgående settings-register bør beskrive stabile **semantiske** ID'er,
datatyper/gyldige værdier, standarder, afhængigheder, ændringseffekt, datascopes,
samtykke og adgangskrav. Det bør også skelne mellem:

- tilgængelig, utilgængelig og kun udviklerfunktion;
- profilglobal, enhedslokal og local-only;
- øjeblikkelig præferenceændring og eksplicit backendhandling;
- konfigureret intention og den faktisk aktive adapter-/OS-tilstand.

Kontrakten skal ikke indeholde SwiftUI-bindinger, WPF-controlnavne, native
stemme-ID'er eller screenshot-pixelmål som universelle sandheder. Hver platform
ejer sin mapping til kontroller, lagring og runtime samt sin teststatus.

En fremtidig konsolidering kan samle nye platformneutrale kontrakter under fx
`WordSuggestorDocs/10_contracts/`. Det er et **forslag**, ikke en allerede udført
omorganisering. Eksisterende autoritative Core-/backend-/packkontrakter beholdes
hos deres ejere; fælles dokumenter linker til dem. Eventuelle flytninger kræver
opdaterede indeks/redirect-henvisninger, så eksisterende links ikke går i stykker.

## Parity og versioner

Mac-billeder er et produktreferencepunkt, ikke et evigt platformneutralt API.
En lokal analyse skal angive referencedato, screenshot-/buildversion, kildecommit
og eventuelle arbejdsmappeændringer. Her sammenlignes macOS `0.9.0-beta.9 (1009)`
med den aktuelle Windows-arbejdsmappe; det betyder ikke, at Windows skal bruge
samme pakkenummer eller allerede har alle Mac-funktioner.

Et fælles overblik kan have én række pr. semantisk funktion og kolonner for macOS,
Windows og senere platforme. Hver celle linker til en lokalt ejet evidensrække.
"Kontrol vises", "indstilling gemmes", "runtime virker", "testet" og "udgivet"
skal være forskellige statusser. En deaktiveret kontrol tæller ikke som fuld paritet.

Nye sprint-ID'er følger det eksisterende `WSA-UX-NNN_<slug>`-format med `windows`
i slug og platform/ejer i dokumentmetadata. Før nummeret tildeles, kontrolleres
de kendte lokale/fælles planer for kollisioner. Eksisterende historiske numre
omdøbes ikke bagudrettet.

## Arbejdsgang

1. En platformsejer opdaterer egen analyse, sprint, kodebeskrivelse, tests og
   ændringsnote. Screenshots med persondata bliver uden for git.
2. Ændringsnoten indeholder en kort konsolideringssektion: hvilke fælles
   produktkontrakter eller statuslinks bør opdateres, og hvem ejer afhængighederne?
3. Ved fælles ændring skriver Core-/backend-/andre platformsejere egne lokale
   kontrakter og ændringsnoter; Windows-noten linker til deres kvalificerede revision.
4. En særskilt docs-konsolidering opdaterer det fælles indeks/parity-overblik. Den
   kopierer ikke hele platformenes sprintplaner og smoke-tabeller.

For Settings-opgaven betyder det: analysen og det kommende Windows-sprint bor
her i `WordSuggestorWindows/docs`. Mac-koden og dens sprint bruges read-only som
reference. Den fælles docs-repo får senere korte links/status og eventuelle
afklarede platformneutrale indstillingskontrakter.

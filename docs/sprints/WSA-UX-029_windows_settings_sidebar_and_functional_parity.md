# WSA-UX-029_windows_settings_sidebar_and_functional_parity

Oprettet/opdateret: 2026-11-10. Platform/ejer: Windows. Status: **Slice 0-værktøjer
og baseline leveret; positiv Hello-/updaterkvalifikation åben**. Settings-UI er
endnu ikke implementeret. Næste uafhængige implementation: slice 1.

## Resultat sprintet skal levere

Windows-indstillinger skal have den genkendelige venstremenu fra macOS og de samme
indstillinger med samme betydning og faktiske effekt. Brugeren skal kunne skifte
kategori, læse en forklaring, ændre indstillinger og se korrekt status uden at
miste skrivefokus, dokumenter, samtykke eller lokale data.

Det er en kontrolleret refaktorering af den eksisterende WPF/.NET 10-app, ikke en
ny solution eller et WinUI-skifte. Den nuværende toolbar, editorfaner, Core-bro,
TTS/STT/OCR og Windows-releaseflow bevares. Kontroller uden runtimeeffekt tæller
ikke som leveret funktionsparitet.

Sprintet er oprettet efter brugerens afklaring af analysens Q1–Q3. Oprettelsen er
en dokumentationsleverance, ikke en påstand om implementeret UI eller beståede
funktionstests. Der er ingen kendte resterende produktspørgsmål fra analysen.
Tekniske undersøgelser og eksterne kvalifikationsgates nedenfor er fortsat arbejde.

## Autoritativ reference og beslutninger

- [Kontrolregister og kildeanalyse](../analysis/2026-11-09_settings_parity_analysis.md):
  11 screenshots og 72 kontrol-/funktionsposter med stabile ID'er.
- Kanonisk macOS-reference:
  `ba18b451372dd2e3f125e557c5db45894f4e1ae3`, `main`, `feat(beta): add secure Sparkle updates`.
  Den 10. november er lokal HEAD, origin/main og `git ls-remote origin refs/heads/main`
  verificeret til samme hash. Kommareferencen fra `26ff632` indgår i denne version.
  Mac-agenten behøver ikke pushe mere kode til analysegrundlaget.
- Produktreference: macOS `0.9.0-beta.9`, build `1009`, i brugerens billeder.
  Skærmbilledernes valgte værdier er ikke nye fabriksstandarder eller samtykker.
- Windows-baseline: `71c342e` plus den eksisterende arbejdsmappe. Før implementation
  registreres den konkrete diff og kvalificerede Core-/packversion; tidligere
  uncommitted faner/releasearbejde må ikke overskrives.

| Beslutning | Fastlagt 2026-11-10 |
|---|---|
| Lokal kontooplåsning | **Windows Hello med PIN eller biometri**, native og med reel beskyttelse af sessionsmateriale. Ikke et krav om kun biometri. |
| Almindelige forslag | Standard **Ctrl+1…0**. |
| Primær fagordsliste | Standard **Ctrl+Shift+1…0**. |
| Sekundær fagordsliste | Standard **Alt+1…0**. |
| Genvejekonfiguration | Valgbare, uafhængige og konfliktkontrollerede; Windows-/app-reserverede kombinationer må ikke stjæles. Ctrl+Tab/Ctrl+Shift+Tab bevares til editorfaner. |
| Legacy | **Åbn de hidtidige Windows-indstillinger under migrationen**. Mac-legacyens neurale runtime-/udviklingsværktøjer tilføjes ikke automatisk. |
| Dokumentation | Kun Windows-ejede implementeringsplaner her. Mac/Core/backend bruges som kvalificerede referencekilder med egne ejere. |

## Mål for navigation og kontroladfærd

Venstremenuen har Profil som kontokort øverst, derefter Generelt, Oplæsning,
Ordforslag, Tekstanalyse, Læring, Fejlsporing og Avanceret. Profil vælges initialt.
Højre flade scroller uafhængigt af venstremenuen og har grupperede rækker med en
fælles værdikolonne. Windows beholder native vinduesramme, kontrolstile, fokus og
systemfarver. Ingen macOS-trafiklys eller kopieret AppKit/AX-kode.

Alle relevante info-knapper åbner tilgængelig hjælpetekst. Kun én forklaring er
åben ad gangen; kategoriskift og Escape lukker den og bevarer et fornuftigt fokus.
Kategorier og kontroller skal kunne betjenes med tastatur og Narrator.

Almindelige præferencer anvendes og gemmes løbende som i referencen. Konto-/profil-
kladder har deres egne eksplicitte handlinger: login, registrering, Gem
profiloplysninger, passwordændring og logud. En global Gem-knap må ikke foregive
at styre disse transaktioner. Ved skrivefejl må UI'et ikke rapportere falsk succes.

Legacy-knappen åbner en adskilt, ikke-rekursiv visning af de eksisterende Windows-
indstillinger. Begge visninger bruger samme settingsautoritet. Gem i en ældre
dialog må ikke overskrive nyere ændringer fra den nye flade eller droppe nye felter;
brug feltafgrænset opdatering/revisionskontrol. Eksisterende Windows-talevalg
bevares, indtil deres migration er dokumenteret og verificeret.

`Synkronisering`/`Udvikler` er ikke nye offentlige kategorier. Eventuelle tekniske
værktøjer følger eksisterende development/release-grænser. Ingen ny upload,
automatisk rankinglæring eller ændret betalings-/licensmodel følger af UI-paritet.

## Ansvar og afhængigheder

| Område | Ejer/grænse | Leverance til Windows |
|---|---|---|
| Windows UI, settings og platformadaptere | Dette sprint, `WordSuggestorWindows` | WPF-visninger, orchestration, sikre Windows-adaptere, test og lokal dokumentation |
| Ranking, normalisering, kilder og portabel analyse | Core-ejer, `WordSuggestorCore` | Versioneret adapterkontrakt/capabilities, relevante optioner/resultater og parity fixtures |
| Konto, profiler og sessionsprotokol | Eksisterende backendkontrakt og backend-ejer | Auth/refresh/verifikation/profil/entitlements/capabilities til Windows-klienten; ingen parallel protokol |
| macOS-semantik | Read-only reference `ba18b45` | Kontrolbetydning, conditional states, persistens-/datascope; ingen Mac-platformkode porteres |
| Windows-installation/update | Eksisterende Windows-releaseflow | MSIX/App Installer, godkendt signing og testmiljø til kvalifikation |

Dependency er ikke det samme som en allerede leveret Core-/backendfunktion.
Windows-ejeren skriver konkrete kontraktbehov, fixtures og forventede svar lokalt
og konsumerer en kvalificeret revision. En kontraktfejl dokumenteres til ejeren.
Dette plantrin ændrer ingen af de andre repos eller produktionsbackend.

## Sliceplan

Alle slices var **Planlagt / ikke implementeret** ved sprintoprettelsen. En slice
kan leveres separat, men sprintets fulde paritetsmål forbliver åbent, indtil alle
kontroller har virkning og evidens. Ingen dato-/tidsestimater er opfundet.

| Slice | Leverance | Afhænger af | Primær accept |
|---|---|---|---|
| 0 | Fastlås baseline og kvalificér risikable platformgrænser | Kilde/produktvalg, afklaret | Git-/pack-/Core-evidens, baseline-gates, komplet kontrolmapping og konkrete resultater for updater-/Hello-adgang |
| 1 | Native venstremenu, grupper, info og Windows-legacyvisning | 0 | Otte kategorier i korrekt rækkefølge; mouse/keyboard/Narrator-navne; ingen tabte eksisterende indstillinger; ingen rekursiv legacy-dialog |
| 2 | Settingsautoritet, migration, lokalisation og Generelt | 0–1 | Separate UI-/skrivesprog, live persistens, scope-/future-schema-beskyttelse og korrekt caret-tilladelse |
| 3 | Core-capabilities og Windows-adapter til settingsfunktioner | 0, kvalificeret Core-leverance | Reelle kontrakter/fixtures for kildevalg, domæner, familier, analyse og læringsbehov; gamle requests virker stadig |
| 4 | Oplæsning med reel strategi og korrekt hastighed | 1–2, taleadaptere; evt. fælles segmentkontrakt | Stemmevalg pr. sprog, 50–150 % i 5 %-trin, ord/sætning/afsnit ved de rigtige grænser, markering og sikker stop/faneskift |
| 5 | Ordforslagsindstillinger og tre genvejelanes | 1–3; hover bruger 4 | Alle F-kontroller påvirker deres egen funktion; samme commit-path, source-/domainparitet og konfliktkontrol |
| 6 | Tekstanalyse, lokale ordvalg og fejlsporing | 1–3; analyse-/fortolkningsresultater | Ægte marker-/korrektionsresultater, dependency-states, lokal saved-choice-visning/sletning og særskilte privacyvalg |
| 7 | Profil, konto og Windows Hello | 1–2; eksisterende backendkontrakter, sikker sessionadapter | Login/registrering/verifikation/profil/password/logout/entitlements med ægte backendstatus og sikker PIN-/biometribeskyttelse |
| 8 | Opdateringsgruppen med reel OS-autoritet | 0–2; eksisterende MSIX/updateflow | Installeret version, ærlig automatisk status/styring og manuel kontrol; ingen falsk lokal toggle |
| 9 | Avanceret, samlet regression og releasekvalifikation | 1–8 | Sikker kategoriseret diagnostik, ukendt-ord-grænse, alle 72 poster verificeret, DPI/accessibility/data- og update-tests dokumenteret |

Status 2026-11-10: slice 0's kontrolregister, gate-runner, kildefingerprints og
native prober er implementeret. Ti baseline-gates passerer på den registrerede
arbejdsmappe. Hello returnerer DeviceNotPresent; OS-opdateringsændring fejler med
0x800706F7 både via cmdlet og native API, med verificeret uændret testtilstand.
[Slice-0-evidens](../qualification/WSA-UX-029_slice0.md) angiver præcis bevisgrænse.
Positiv platformgodkendelse er ikke lukket; slice 7/8 er fortsat gated. Slice 1–9
er ikke implementeret. Slice 1 kan starte uden at aktivere disse kontroller.

### Slice 0 — konkret risikokvalifikation

Genkør relevante eksisterende gates mod arbejdsmappekoden; tidligere PASS er
baselinehistorik, ikke bevis for den kommende Settings-version. Brug isoleret
artifact-output, hvis en brugerinstans låser standardmappen; ingen tvangslukning.

Updateren skal undersøges på understøttede Windows-versioner og som almindelig
bruger: læs/ændr/genlæs den faktiske OS-præference. Windows 11-API'er og nødvendige
capabilities må ikke antages at virke på Windows 10 eller med nuværende manifest.
Bevar Windows 10 2004+/Windows 11-minimum, indtil en konkret produktbeslutning
eventuelt ændrer det. En ægte, dokumenteret begrænsning skal forelægges, før G05
omdefineres eller erklæres leveret. Kopiér ikke Sparkle til Windows og indfør ikke
et parallelt usigneret updateflow.

Hello-undersøgelsen skal bevise nøgle-/tokenbeskyttelse og sessiongendannelse,
ikke kun vise en PIN-dialog. Kræv afgrænsede testresultater for cancel, låsning,
ikke-provisioneret Hello og fravær af hardware. Et VM/RDP-miljø kan teste
utilgængelighed, men kan ikke alene attestere en vellykket fysisk biometriflow.
PIN er accepteret af produktvalget; biometri er en yderligere vej, når tilgængelig.

### Slice 2–3 — datakontrakt og fælles funktionalitet

Bevar `%LOCALAPPDATA%\WordSuggestor`, brugerord, packs, editorfaner og kladder.
Versionsstyr settingsmigrering med backup, atomisk skrivning og beskyttelse af
fremtidige formater. Bevar udtrykkeligt false/ukendt samtykke og teksteksempler fra
som standard. UI-sprog må ikke udledes af skrivesprog. Mac-stemme-ID'er eller
Mac-modifier-råværdier må ikke anvendes direkte som Windows-systemidentiteter.

Core JSON v1 kan nu kun `suggest` og `addUserWord`. Udvidelser skal være aftalte,
versions-/capability-styrede og testet mod gammel klientadfærd. Kildevalg må ikke
implementeres ved vilkårlig C#-efterfiltrering, og ingen lingvistiske regler må
flyttes fra Swift/Core ind i UI'et. Resultater skal knyttes til request-ID,
dokument/tab-ID og tekstgeneration, også ved identisk tekst i to faner.

### Slice 4–6 — skrivehjælp og lokal læring

Adskil oplæsningsstrategi under skrivning fra manuel TTS og fra highlight-valg.
Hastighedslabel, gemt værdi og native afspilning skal være samme brugerbetydning;
gammel Windows-rate skal migreres med dokumenteret normalisering. Stop/annullér
arbejde ved faneskift, så lyd, analyse eller commits ikke går til et andet dokument.

De fem responsivitetsprofiler skal ændre planlægning af forespørgsler og måles
under hurtig skrivning. Fastlæg baseline og rapportér målinger; påstå ikke et
latenstidsmål uden måling. Bevar ti kandidater pr. side og højst fire sider.
Genveje registreres kun i den rette sessionskontekst, frigives igen og håndterer
fejlet registrering uden at stjæle andre programmers genveje. Alt/AltGr og dansk
tastaturlayout skal med i test. Keyboard og mouse bruger samme verificerede commit.

Analyse/komma/familieforslag bruger den fælles enginegrænse og UTF-16-ranges.
"Gemte ordvalg" er fortolkningsvalg til evaluering, ikke brugerordslisten eller
automatisk rankinglæring. Gemte valg viser begrænset lokal information med
Tilbage/slet én/bekræftet slet alle. Nulstil læring rydder det dokumenterede scope,
ikke dokumenter eller brugerord. Delingssamtykke skaber ikke i sig selv en ny upload.

### Slice 7–9 — konto, opdatering og diagnostik

Konto bruger eksisterende backendcapabilities og tilstande, inklusive udløbet,
offline-gendannelig, låst, in-flight og behov for nyt login. Profiloplysninger er
kladder med eksplicit Gem og backendbekræftet status. Passwordændring kræver
nuværende password også med Hello. Skeln mellem lokal/all-device logud og beskyt
mod dobbelte requests. Vis kun abonnement som den eksisterende autoritative status.
Test med mock/lokal eller særskilt testkonto; ikke produktionsmutationer uden scope.

App Installer ejer signatur/installation. Kontrol, download, installation og
genstart skal holdes adskilt; aktiv skrivning og åbne kladder skal beskyttes.
Automatisk-checkboxen må ikke love fravalg af OS-opdatering, hvis kun en app-bool
ændres. Offline/forkert feed/identity/publisher/signatur skal fejle roligt.

Avanceret leverer særskilte debugkategorier og afgrænset performanceinstrumentering.
Panel/AX oversættes meningsfuldt til Windows Panel/UI Automation/caret. Rå tekst,
passwords, tokens og rå Core-stderr må ikke begynde at optræde i diagnostik.
Ukendt-ord-læring er ikke manuel tilføjelse af brugerord og har sit eget samtykke.

## Komplet kontrol-til-slice-sporing

Den fulde betydning af hver post står i analysen. Denne tabel angiver primær
implementationsleverance; alle poster starter som **ikke implementeret/verificeret
i dette sprint**. UI-shell i slice 1 og data i slice 2 gælder på tværs.

| Register | Poster | Primær slice | Påkrævet evidens |
|---|---|---|---|
| Profil | P01–P21 | 7 | Conditional konto-/sikkerhedsflows, ægte handlinger/fejl, lokal sikker lagring, genstart/offline |
| Generelt, sprog/caret | G01–G03 | 2 | To sprogautoriteter, localization, tilladelse vs. aktiv placering og statisk fallback |
| Generelt, opdatering | G04–G06 | 8 | Kørende version, faktisk OS-præference og manuelt updateflow |
| Oplæsning | O01–O05 | 4 | Stemme, systemindstillinger, hastighed, skrive-strategi og markering |
| Ordforslag | F01–F20 | 5 | Hver indstillings engine-/panel-/editor-/commit-effekt og konfliktfri genveje |
| Tekstanalyse | T01–T06 | 6 | Analyse/cancellation, afhængige kontroller, POS/semantik/tegnsætning og begge kommastile/policyveje |
| Læring | L01–L04 | 6 | Samtykke, lokal signalstore, detalje, sletning og præcis nulstilling |
| Fejlsporing | E01–E02 | 6 | Master/afhængighed, tællere og udtrykkeligt valgte lokale teksteksempler |
| Avanceret, diagnostik/læring | A01–A07 | 9 | Faktisk toggle-effekt, privacy og unknown-word-kontrakt |
| Avanceret, legacy | A08 | 1 | Hidtidig Windows-visning, fælles autoritet, ingen nye Mac-neuralværktøjer |

## Test- og evidensplan

Teststatus ved oprettelse: **ikke kørt for sprintets implementation**. Slice 0 har
nu sin egen [baseline og platformrapport](../qualification/WSA-UX-029_slice0.md).
Den er ikke PASS-evidens for fremtidige Settings-kontroller. Analysens statiske
validering og tidligere app-tests må ikke omdøbes til nye feature-PASS-resultater.

| Testgruppe | Minimum |
|---|---|
| UI-register | Otte kategorier/rækkefølge, alle 72 poster inkl. conditional flows, hjælpetekster, default-/enabled-state og ingen usynlige tabte legacyvalg |
| Lokal settingsautoritet | Live save, atomisk persistens, filfejl, concurrent legacyændring, genstart, migration, fremtidigt format og rollback |
| Core-kontrakt | Real Core/DA, kildevalg, profiler/domæner, analyse/familier, repræsentative Mac/Windows-fixtures med samme revision/pack; timeout/cancel/stale/no-echo |
| Skriveintegration | Intern editor med flere faner og Notepad; mouse/keyboard-samme commit, præfikserstatning, caret, ingen dobbeltcommit og sikkert fokus-/targetskift |
| Genveje | Tre lanes, kollisions-/registreringsfejl, AltGr/Unicode, paging samt bevaret Ctrl+Tab/Ctrl+Shift+Tab og almindelig Windows-betjening |
| Tale | Systemstemmer/fallback, 50–150 %-skala, reelle ord-/sætnings-/afsnitstriggere, hover-cancel, markering, stop/faneskift |
| Konto/Hello | Login/registrering/verifikation, sessionrefresh/race, profil/password/logud, offline/låst, PIN og tilgængelig biometri samt cancel/lockout/ikke-provisioneret |
| Privacy/læring | Opt-in bevares, ingen implicit upload, korrekt slette-/reset-scope, ingen rå bruger-/credentialdata i logs |
| Update/install | Almindelig bruger, Win10/Win11, unpackaged/MSIX, reel auto-præference, manuel check, offline/forkert feed og installeret N→N+1 med settings/packs/kladder bevaret |
| Manuel UX | Mouse/keyboard/Narrator, høj kontrast, relevante vinduesstørrelser, 100/125/150/200 % DPI og flere skærme; sammenligning med S01–S11 |

Brug de eksisterende scripts. Fra Windows-repoet er de relevante kommandoer:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_run_scripts.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_release_scripts.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_clean_checkout.ps1 -IncludeWorkingTree
```

Udvid eksisterende test-/releaseflow, og brug dets MSIX-/release-candidate-tests,
når slice 8–9 er klar. `-IncludeWorkingTree` eksporterer den aktuelle kilde uden at
kræve en commit; pin Core-input som i den eksisterende lockfil eller en konkret
kvalificeret opdatering. Isoleret build bruger `-ArtifactsPath`; GUI-smoke kræver
ulåst desktop og et aftalt testvindue. Signeret installation/Hello-hardware og
backendtestadgang er eksterne testforudsætninger, ikke noget der allerede er tilgængeligt.

## Definition of done og leverancegrænser

- Alle 72 registerposter har dokumenteret UI, semantik, adaptereffekt og konkret
  testresultat; midlertidigt deaktiverede/no-op-kontroller er åbne punkter.
- Venstremenu, gruppering og kontroller svarer til den kanoniske Mac-reference
  med de tre udtrykkeligt valgte Windows-tilpasninger.
- Eksisterende funktioner og brugerdata er bevaret eller migreret med verificeret
  tilbagevej. Hverken legacy-Gem eller rollback ødelægger nyere settings.
- Core-/packformat/ranking er fortsat fælles. Ingen ny C#-lingvistik, usikker
  tekstinjektion, separat backend eller ændret betaling.
- Auth/Hello og OS-updater er reelle og testede; sikkerheds-/OS-begrænsninger
  fremgår, og eventuelle nødvendige produktændringer er afklaret før closeout.
- Automatiske gates passerer; manuel/hardware-/installer-/backendevidens angiver
  præcist gennemført, ikke gennemført eller konkret afvigelse. Ingen fiktiv sign-off.
- Windows README/Plan/Architecture/ParityMatrix/UiParityPlan/ManualSmoke og daterede
  ændringsnoter afspejler det faktisk leverede. Andre platformes docs ændres ikke
  som en sideeffekt; fælles konsolidering er separat.
- Diff er gennemgået for hemmeligheder, binærfiler og uvedkommende ændringer.
  Commit/push/publicering kræver den gældende brugerautorisation; dette sprint
  giver ikke i sig selv tilladelse til produktionspublicering.

## Status og næste handling

Produkt-/referenceafklaringer: afsluttet. Slice 0: kvalifikationskode/baseline
leveret, positiv Hello-/OS-updategodkendelse åben. Settings-UI: ikke startet.
Tekniske risici: updaterstyring på tværs af Windows-versioner, Hello-beskyttelse,
Core-adapterudvidelser, analyse/fortolkningsdata og sikker ekstern assist.

Næste uafhængige arbejde er slice 1: byg venstremenuen og Windows-legacyvisningen.
Parallelt i kvalifikationsrækkefølgen kræves en Hello-provisioneret testsession
med en konkret sikker adapter samt et vellykket OS-update-roundtrip på relevante
understøttede miljøer. Den registrerede hostfejl omdefinerer ikke G05 til en falsk
lokal toggle. Brugeren vil først ved slice 1 se det nye layout; slice 0 aktiverer
ingen af de manglende produktfunktioner.

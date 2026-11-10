# Analyse: Settings-paritet for Windows

## Canonical-source correction — 2026-11-11

Detailed inspection of `GlobalKeyCaptureManager` at canonical `ba18b45` narrows
F10: **English only**, direct uppercase after a recent newline, starter/token
thresholds and connector exclusions. It is not general uppercase rewriting or
a Danish sentence heuristic. F03 Smart also has a verified 1.5-second followup
receipt, rather than only inserting a trailing space. These findings supersede
broader interpretations in the original control register below. The Core-owned
portable policy and [Windows host contract](../Suggestions.md) implement that
boundary; Windows external period rewriting remains disabled/unqualified.

Analyseret: 2026-11-09. Afklaringer afsluttet: 2026-11-10. Ejer: Windows-sporet.
Status: kildeanalyse og produktvalg afsluttet; [WSA-UX-029 er oprettet](../sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md).
Der er ikke ændret appkode i analyse-/planopgaven. Slice 0 har efterfølgende
[leveret kvalifikationsværktøjer og baseline](../qualification/WSA-UX-029_slice0.md),
med åbne positive platformgates. Settings-UI er ikke implementeret.

## Formål og konklusion

Windows skal have venstrenavigation og samme kategorier, grupper, kontroller og
funktionsbetydning som den aktuelle macOS-reference. Windows beholder WPF/.NET 10
og bruger native Windows-kontroller, tastaturbetjening, vinduesramme og accessibility.
Det eksisterende settings-vindue videreføres gennem en kontrolleret refaktorering.

Dette er både en UX- og en integrationsopgave. En flytning af de nuværende vandrette
faner vil ikke give funktionsparitet. Profil/backend, læringssignaler, flere
forslagsfunktioner og analysekontrakten mangler i Windows. En synlig eller gemt
indstilling er ikke bevis for, at dens funktion virker. Sprintets omfang og
acceptkriterier står i den særskilte Windows-sprintplan.

## Referencegrundlag og sporbarhed

- Windows: `71c342edd7b0b2aa89ced5ffc6b9f36e1332dbfd` **plus den eksisterende
  arbejdsmappe**, som bl.a. indeholder faner og releasearbejde. Git-status er
  kontrolleret; disse ændringer bevares. Analysen beskriver arbejdsmappekoden.
- macOS: `ba18b451372dd2e3f125e557c5db45894f4e1ae3`, dateret 2026-11-05.
  Den lokale reference stod først på `b581de0` fra 19. oktober. Under analysen
  blev den nyere reference tilgængelig, og de manglende opdaterings-/kommakontroller
  blev genkontrolleret. Den tidlige kildeforespørgsel er dermed afklaret.
  Checkout/arbejdsfiler blev ikke ændret af analysen; `git fetch origin` blev kørt.
  Den 10. november har brugeren bekræftet `ba18b45` som kanonisk reference.
  Lokal HEAD, origin/main og et nyt `git ls-remote origin refs/heads/main` er nu
  alle verificeret til den fulde hash; switch/merge var derfor ikke nødvendigt.
- macOS-ændringsnoten `docs/changes/2026-11-03_hidden_shell_startup_flash_fix.md`
  identificerer den officielle `0.9.0-beta.9`, build `1009`, som billederne viser.
  Binærens præcise kildehash er ikke uafhængigt attesteret her.
- Windows Core-grænse: `WordSuggestorCore/Tools/SuggestCLI/StdioProtocol.swift`
  og Windows' `docs/BridgeContract.md`. Ingen Core-kode er ændret.

Billederne er gennemgået som brugerleveret reference. De indeholder personlige
kontodata; de kopieres ikke ind i git. Eksempler i fremtidige fixtures skal være
syntetiske. Indstillingerne på billederne er brugerens aktuelle værdier, **ikke**
en specifikation af fabriksstandarder eller nyt samtykke.

| Reference | Filnavn i det leverede materiale | Dækker |
|---|---|---|
| S01 | `Screenshot 2026-11-09 at 22.22.52.png` | Profil, udlogget/udløbet forbindelse |
| S02 | `Screenshot 2026-11-09 at 22.23.03.png` | Generelt |
| S03 | `Screenshot 2026-11-09 at 22.23.10.png` | Oplæsning |
| S04 | `Screenshot 2026-11-09 at 22.23.23.png` | Ordforslag, øvre del |
| S05 | `Screenshot 2026-11-09 at 22.23.35.png` | Ordforslag, nedre del |
| S06 | `Screenshot 2026-11-09 at 22.23.43.png` | Tekstanalyse |
| S07 | `Screenshot 2026-11-09 at 22.23.45.png` | Læring |
| S08 | `Screenshot 2026-11-09 at 22.23.54.png` | Fejlsporing |
| S09 | `Screenshot 2026-11-09 at 22.24.00.png` | Avanceret |
| S10 | `Screenshot 2026-11-09 at 22.40.34.png` | Profil, indlogget, øvre del |
| S11 | `Screenshot 2026-11-09 at 22.40.43.png` | Profil, indlogget, nedre del |

Følgende symboler er de primære kildeankre i macOS-repoet; de skal læses ved det
anførte commit, hvis filernes linjenumre flytter sig:

| Kilde | Anvendelse |
|---|---|
| `WordSuggestor/SettingsNavigationPolicy.swift` | Release-kategorier, rækkefølge, udviklergrænse, profilnavn og monogram |
| `WordSuggestor/SettingsView.swift`: `macSystemSettingsLayout`, `macSettingsSidebar`, `MacSettingRow`, `macCategorySections` | Navigation, kontrolbindinger, afhængigheder og layout |
| Samme fil: `signedOutProfileContent`, `signedInProfileContent`, `backendSessionSecurityGroup`, `macSavedWordChoicesDetail`, `systemVoicePicker` | Konto-/sikkerhedsflows, gemte valg og stemmevalg |
| `WordSuggestor/SettingsInterfaceCopy.swift` | Danske/engelske labels, hjælpetekster og værdinavne |
| `WordSuggestor/AppState.swift` | Effekter ved ændring, persistens, Core-routing, tale, signaler og backend |
| `WordSuggestor/SettingsProfile.swift`, `BackendControlPlaneContracts.swift` | Settingsformat, samtykke og lokal/global klassifikation |
| `WordSuggestor/SpeechSpeedPolicy.swift`, `TypingReadAloudDetector.swift` | Hastighed og automatisk oplæsning under skrivning |
| `WordSuggestor/SuggestionPanelRowDensity.swift`, `SuggestionSelectionShortcutScheme.swift`, `FollowCaretPlacementOptionPolicy.swift` | Rækkedetaljer, genveje og caret-tilladelse |
| `WordSuggestor/ContextualCommaRolloutPolicy.swift`, `TextAnalyzer.swift` | Kontekstuelle kommaer og analyseorkestrering |
| `WordSuggestor/LocalLearningSignalStore.swift`, `BackendSessionBiometricPolicy.swift`, `BackendSessionCoordinator.swift` | Lokal læring og sessionens sikkerhed |
| `WordSuggestor/AppUpdateController.swift` | Den autoritative Sparkle-controller og update-præference |

## Vindue, navigation og ændringssemantik

Målstrukturen er Profil som særskilt kontokort øverst, derefter Generelt,
Oplæsning, Ordforslag, Tekstanalyse, Læring, Fejlsporing og Avanceret. Profil er
initialt valgt i macOS-koden. `Synkronisering` og `Udvikler` er ekstra
**udviklerkategorier**, ikke manglende kategorier på brugerens releasebilleder.

Venstremenuen bevarer sin position, mens højre detaljeflade scroller. Grupper har
overskrift, adskilte rækker, forklaring til venstre og kontrol/værdi i en fælles
højre kolonne. Mac-kilden bruger 280/24/340-point label/mellemrum/værdikolonne;
det er reference for alignment, ikke et krav om identiske Windows-pixels.
Windows-layoutet skal fungere ved skalering, høj kontrast og tastaturnavigation.

Info-ikonerne er handlinger: klik viser label og hjælpetekst, samme ikon lukker,
en anden info-handling erstatter den åbne forklaring, og kategoriskift lukker den.
Tooltip alene er ikke fuld paritet. Windows skal også give keyboard/Narrator adgang.
Blå markering betyder valgt kategori, ikke en anden produktfunktion.

Mac-kontroller skriver normalt straks gennem AppState og persisterer; der er
ingen fælles Gem/Annuller-bjælke. Profilfelter er derimod kladder med eksplicit
Gem profiloplysninger. Login, adgangskodeskift, nulstilling og logud er selvstændige
handlinger. Windows bruger nu en klonet snapshot med Gem/Annuller. Den semantiske
forskel skal med i implementeringen og i regressionstests for skrivefejl/offline.

## Kontrolregister: Profil

Windows har ingen tilsvarende konto-/profilflade eller backend-session i dag.
Alle rækker her kræver en reel Windows-konto-/sessionadapter til den eksisterende
backendkontrakt; en lokal boolsk "indlogget"-tilstand er ikke tilstrækkelig.

| ID | Komponent | Funktion afdækket i koden / betingelser |
|---|---|---|
| P01 | Profilkort og identitet | Navn/monogram bruger backendprofil med e-mail-fallback. Aktiv session afgør indlogget/udlogget flade; en husket e-mail er ikke loginbevis. |
| P02 | Konto-adgang: Log ind / Opret konto | Skifter mellem login og registrering. Registrering er capability-gated, ikke ubetinget aktiveret af UI'et. |
| P03 | E-mail og adgangskode | Login bruger eksisterende backendklient/sessionkoordinator. Password må ikke persisteres eller logges. |
| P04 | Bekræft adgangskode ved oprettelse | Betinget felt i registreringstilstand; validering og e-mail-bekræftelsesflow følger backend. |
| P05 | Login-/registreringsknap og status | Asynkron handling med in-flight-beskyttelse, fejl og verifikation/pending-state. Udløbet/ikke-gendannelig session beder om login igen. |
| P06 | Verifikation og gensendelse | Capability-/statusstyrede kort, bekræftet e-mail og gensendelses-cooldown. Handoff fra browser må ikke selv attestere verifikation. |
| P07 | Brug Touch ID | Skifter mellem beskyttet lokal session og automatisk sessiongendannelse; treværdipolitikken bevarer også legacy `loginAtLaunch`. Slået fra er ikke logud og slår ikke TLS fra. Q1: Windows Hello med PIN eller biometri er vedtaget. |
| P08 | Profilnavn | Redigerbar kladde; afsendes med Gem profiloplysninger. |
| P09 | Aldersgruppe | Valgfri backendværdi fra `ProfilePersonalizationPresentationPolicy.ageBandIDs`; bl.a. under 18, intervaller og ønsker ikke at oplyse. |
| P10 | Profession | Valgfrit katalog-ID, ikke en frit opfundet Windows-tekstliste. |
| P11 | Branche | Samme princip; eksisterende backendkatalog og uspecificeret værdi bevares. |
| P12 | Tillad fremtidig profiltilpasning | Samtykke-/metadataværdi i profilkladde. Kilden siger udtrykkeligt, at oplysningerne endnu ikke ændrer forslag eller fagordslister. |
| P13 | Gem profiloplysninger | Sender de fem profilværdier via `saveBackendProfilePersonalization`; viser success/failure og in-flight. Ingen automatisk tilpasning af ranking. |
| P14 | E-mail og E-mail bekræftet | Autoritativ backendstatus; read-only værdi/indikator. |
| P15 | E-mailændring, hvis tilgængelig | Ekstra capability-gated felter/flow i kilden, ikke på billederne: ny e-mail, bekræftelse og nuværende password samt pending/cancel/status. |
| P16 | Nuværende, ny og bekræft ny adgangskode | Nye passwords skal matche; minimumspolitik vises, backend er autoritet. Den aktuelle adgangskode kræves også med lokal biometri. |
| P17 | Skift adgangskode | Særskilt backendhandling, ikke del af profil-Gem. Rydder sikkerhedskladder ved succes og håndterer sessionsinvalidering/genlogin. |
| P18 | Abonnement | Read-only backendentitlement: niveau/status/evt. gyldighedsdato; ukendt vises som utilgængelig. Ingen ny betalingsmodel. |
| P19 | Log ud på denne enhed | Revokerer/rydder den relevante session. Windows-label skal sige denne pc, ikke denne Mac. Lokale dokumenter er uafhængige. |
| P20 | Log ud på alle enheder | Destruktiv kontooperation med bekræftelse, separat fra lokal logud. |
| P21 | Forbindelses-/fejltilstande | Understøtter bl.a. låst, offline-gendannelig, udløbet, in-flight og behov for nyt login. Lokale forslag må fortsat virke uden backend. |

## Kontrolregister: Generelt (S02)

| ID | Komponent | macOS-adfærd | Windows nu / nødvendigt arbejde |
|---|---|---|---|
| G01 | Interface-sprog | Systemstandard, dansk eller engelsk; særskilt fra skrivesprog, UI-tekst skifter gennem lokaliseret copy. | UI er i praksis dansk; tilføj separat præference og dansk/engelsk lokalisation. |
| G02 | Skrive-sprog | Samme sprogautoritet og pack-katalog som toolbar; ændrer engine/analyse/stemmegrundlag. | Findes som Aktivt sprog; genbrug adapter, omdøb/flyt. Installerede kompatible packs afgør tilgængelighed. |
| G03 | Tillad Følg markør ... | Tilladelse til at eksponere panelvalget. Fra normaliserer til statisk og stopper caret-arbejde. Til betyder ikke automatisk, at follow vælges. | Har direkte statisk/follow-valg, men ikke separat tilladelse. Opdel intention, tilgængelighed og effektiv tilstand; behold statisk fallback. |
| G04 | Installeret version | Læser den kørende bundles visningsversion og build. | Version/package-label findes i separat opdateringsfane; flyt til Generelt. Ingen hardcoded macOS-version. |
| G05 | Søg automatisk efter opdateringer | Skriver direkte til Sparkles autoritative præference; ikke en dubleret AppState-bool. | App Installer ejer opdatering, ingen styrbar toggle i appen. Kræver OS-adapter og bevis for faktisk effekt; se updater-afsnittet. |
| G06 | Søg nu ... | Sparkle checker og håndterer signatur/netværk/version og videre brugerflow. | Manuel HTTPS-metadata-kontrol og OS-installeråbning findes; genbrug med samme tydelige pending/offline/fejlsemantik. |

## Kontrolregister: Oplæsning (S03)

| ID | Komponent | macOS-adfærd | Windows nu / nødvendigt arbejde |
|---|---|---|---|
| O01 | Stemme | Installerede kompatible systemstemmer for skrivesproget, eksakt locale/quality/navn-sortering, Automatisk (bedst), override pr. sprog. Tom liste giver reel fejltilstand. | OneCore/SAPI-katalog og override findes; genbrug og kontrollér samme sprog-/fallbacksemantik. Stemme-ID'er er platformspecifikke. |
| O02 | Åbn systemindstillinger for oplæsning | Åbner macOS' taleindstillinger med fallback. | Åbn Windows' tale-/sprogindstillinger; eksisterende URI-adaptere kan genbruges. |
| O03 | Oplæsningshastighed | `SpeechSpeedPolicy`: 50–150 %, 5 %-trin, 100 % standard; gemt som normaliseret delta -0,5…0,5. | UI tilbyder -2…2, indlæsning clampes -1…1, faktisk rate bruger bl.a. `1.35^delta`. Labels og effekt er ikke samme kontrakt. Normalisér/migrér og kalibrér SAPI/OneCore. |
| O04 | Oplæsningsstrategi | Automatisk oplæsning under skrivning: Ingen, Læs ord, Læs sætning, Læs afsnit. Ord ved ordgrænse; sætning ved `. `; afsnit ved punktum+linjeskift. Legacy Læs side/alt migreres til afsnit. | Seks værdier vises/gemmes, men `ReadingStrategy` har ingen forbruger i skriveflowet. Reel adfærd mangler. Dette er ikke blot størrelsen på manuelt valgt TTS-tekst. |
| O05 | Markering under oplæsning | Ingen, Ord, Sætning; styrer oplæsningsmarkering. | TTS/highlight-adapter findes. Test stemmegrænser, tabskift, stop og gendannelse af brugerens markering. |

Windows-specifikke eksisterende talevalg må ikke forsvinde ubemærket. Især
`UseSystemSpeechSettings` deaktiverer i dag stemme-/hastighedsvalg og kan gøre en
ny UI-kontrol virkningsløs. Q3 er afklaret: bevar disse Windows-valg i den
eksisterende legacyvisning, indtil deres migration er verificeret.

## Kontrolregister: Ordforslag (S04–S05)

| ID | Komponent | macOS-adfærd | Windows nu / nødvendigt arbejde |
|---|---|---|---|
| F01 | Aktiver ordforslag | Binder til global capture; lokal panelsynlighed/typing er særskilte tilstande. | Global capture-setting findes; test intern/ekstern semantik frem for at antage total engine-stop. |
| F02 | Detaljer i ordforslagsrækker | `wordOnly`, `wordAndOrigin`, `fullDetails`; fast højde pr. mode, ikke afhængig af kandidatmetadata/antal. Mac-højder 30/34/42 points. | Kun eksisterende standardvisning. Tilføj tilsvarende modes, tilgængelige metadata og stabil panelstørrelse. |
| F03 | Mellemrum efter valgt ord | Smart, Altid, Aldrig; Smart har tegnsætnings-/suffixadfærd. | Fast commit-politik nu; udvid den fælles lokale/eksterne commit-path, ingen separate mouse-/keyboardregler. |
| F04 | Responsivitet ved hurtig skrivning | Fem profiler, indeks 0–4, balanceret standard; påvirker timing/backpressure under sammenhængende skrivning, ikke kandidatantal. | Fast debounce/scheduling. Tilføj timingpolitik og mål responstid/stale-resultater; ingen C# ranking. |
| F05 | Genvej: ordforslag | Valg mellem tre modifierfamilier; også paging og intern/ekstern accept. | Ctrl+1…0 er fast nu. Q2 har fastlagt valgbare Ctrl/Ctrl+Shift/Alt-lanes med konfliktkontrol som mål. |
| F06 | Genvej: fagordsliste 1 | Primær specialiseret lane har uafhængig modifier. | Lane og konfigurerbar genvej mangler. |
| F07 | Genvej: fagordsliste 2 | Sekundær lane, samme kontrakt. Kilden normaliserer kolliderende lane-schemes. | Samme mangel; Windows-tast må ikke overtages til formålet. |
| F08 | Oplæs ved mouse-over | TTS ved hover over kandidat. | Eksplicit kandidat-audio findes; hoverindstilling/hover-lifecycle mangler. Test debounce/cancel og fokus. |
| F09 | Ordformer og ordklasser (eksperimentel) | Familie-/bøjningspopup og forskellige fortolkninger af samme stavemåde; kan mindske diversitet ved korte præfikser. | Familie-/variantkontrakt og præsentation mangler; må ikke simuleres med C# bøjningsregler. |
| F10 | Live punktum-assist (beta) | Kan indsætte manglende punktum ved sandsynlig ny sætningsstart; særskilt fra analysediagnoser. | Mangler. Kræver fælles sproglig beslutning og verificeret target/transaction ved ekstern omskrivning. |
| F11 | Fade ud efter inaktivitet | 0–30 sekunder; særværdien 31 betyder Aldrig. Kildestandard er 7, ikke billedets brugerindstillede 15. | Ingen konfigurerbar idle-fade. Må ikke vække/stjæle fokus eller miste pending input. |
| F12 | Kontekst-baserede forslag | Offline n-gram reranking ud fra forudgående ord i aktiv pack. | Windows kan ikke styre det gennem den nuværende request. Udvid fælles enginegrænse og fixtures. |
| F13 | Forvekslingsforslag | Ændrer engineprofilens source-præferencer. | Ikke eksponeret i bridge. |
| F14 | Fonetiske forslag | Samme, særskilt kilde. | Ikke eksponeret i bridge. |
| F15 | Stavefejlsforslag | Samme, kuraterede rettelseskandidater. | Ikke eksponeret i bridge. |
| F16 | Synonymforslag | Samme, synonymkilde. | Ikke eksponeret i bridge. |
| F17 | Fagordslister | Slår specialiserede lanes til/fra, ikke en administrations-placeholder. | Synlig men deaktiveret; den nuværende tekst om, at macOS også blot er placeholder, er forældet. |
| F18 | Primær fagordsliste | Pack-/sprogafhængigt domænekatalog; ingen valgt/ingen tilgængelige; disabled når funktionen er fra. | Katalog, valg, lane-kontrakt og engine-routing mangler. |
| F19 | Sekundær fagordsliste | Kræver primær; ekskluderer samme domæne; ryddes/normaliseres når primær fjernes. | Samme mangel. |
| F20 | Ombryd linjer i intern editor | Styrer editorens wrap-adfærd. | Wrap er nu fast; tilføj gemt indstilling og korrekt vandret scrolling uden at ødelægge caret/markering/faner. |

## Kontrolregister: Tekstanalyse (S06)

| ID | Komponent | macOS-adfærd | Windows nu / nødvendigt arbejde |
|---|---|---|---|
| T01 | Aktiver tekstanalyse | Master for dokumentanalyse; ændring styrer tasks/resultater. | Setting gemmes, men UI er deaktiveret, og reel analysebro mangler. |
| T02 | POS-farvemarkering | Styrer visning af Core-/analysebaserede ordklasser; kræver T01. | Gemt/deaktiveret. Ingen genindførelse af lokale suffixgæt. |
| T03 | Semantiske kontekstdiagnoser | Styrer semantiske analyser; kræver T01. | Gemt/deaktiveret, ingen funktionel bridge. |
| T04 | Tegnsætningsdiagnoser | Styrer tegnsætningsanalyse; kræver T01. | Gemt/deaktiveret, ingen funktionel bridge. |
| T05 | Kommastil | Grammatisk med/uden startkomma; kræver T01+T04. | Model har `Standard`, ingen synlig picker eller tilsvarende runtime. Der kræves eksplicit migration til gyldige semantiske værdier. |
| T06 | Kontekstuelle kommaforslag | Skifter `boundary_local_contextual_v1`/`legacy_graph_only`; kræver T01+T04. Default uden gemt værdi er contextual, ugyldig gemt værdi fejler mod legacy. | Mangler. Skal kobles til fælles analysepolitik, ikke blot en generisk "komma til/fra"-bool. |

`TextAnalyzer.swift` er aktuelt app-lokal macOS-orkestrering over Core-begreber.
At Core genbruges af appen betyder derfor ikke, at alle analysefunktioner allerede
er tilgængelige for Windows. Portabel analyse og resultat-/markør-/korrektionsformat
skal afgrænses i Core-grænsen. AppKit-/AX-kode og sproglige regler må ikke kopieres
til C#. Resultater skal knyttes til dokument, revision og UTF-16-ranges, især med
flere editorfaner og cancellation.

## Kontrolregister: Læring (S07)

| ID | Komponent | macOS-adfærd | Windows nu / nødvendigt arbejde |
|---|---|---|---|
| L01 | Gem ordvalg på denne enhed | Tillader lagring af valg mellem fortolkninger/ordklasser til senere evaluering; ændrer ikke automatisk ranking. Fra slår også delingssamtykke fra. | Ingen tilsvarende signalstore/ambiguity-valgflow. Windows-brugerord er en anden datatype/funktion. |
| L02 | Tillad deling af ordvalg | Kræver lokal lagring. Opdaterer samtykkestatus og præferencer; hjælpeteksten siger, at valgene endnu ikke uploades. | Mangler. Der må ikke indføres ny upload eller arves samtykke fra et screenshot. |
| L03 | Gemte ordvalg: antal og detalje | Navigerer inden for Læring til liste med ord, valgt fortolkning, lokal status og tidspunkt, Tilbage, slet én og bekræftet slet alle. | Mangler; ikke en genvej til brugerordslisten. Visningen viser ikke den omgivende dokumenttekst. |
| L04 | Nulstil læring | Nulstiller lærings-/upload-/fetch-/historikpræferencer og sletter lokale læringssignaler. Det er ikke kun nulstilling af afkrydsninger, og sletter ikke automatisk dokumenter eller brugerord. | Mangler. Destruktiv adfærd, feedback og præcist scope skal testes. |

Den lokale signalmodel indeholder desuden privacy-/eligibility-felter og
kontekstmetadata. UI'ets begrænsede visning er ikke bevis for et tomt underliggende
datasæt. Genbrug kontrakten efter gennemgang af præcist hvilke data der gemmes;
hold rå dokumenter, diagnostik, brugerord og samtykke adskilt.

## Kontrolregister: Fejlsporing (S08)

| ID | Komponent | macOS-adfærd | Windows nu / nødvendigt arbejde |
|---|---|---|---|
| E01 | Aktiver fejlsporing | Master for lokale fejl-/skriveindsigter, ikke master for alle tekniske logs eller al backenddeling. | Funktionel lokal insights-store findes. Genbrug uden at love Mac-analysekategorier, før de produceres. |
| E02 | Gem sætningseksempler lokalt | Opt-in til brugerindhold; disabled når fejlsporing er fra; særskilt fra deling. | Felt og store-flag findes, UI er deaktiveret, standard false. Tilføj udtrykkelig aktivering og verificér privacy/lagring uden at tænde ved migration. |

## Kontrolregister: Avanceret (S09)

| ID | Komponent | macOS-adfærd | Windows nu / nødvendigt arbejde |
|---|---|---|---|
| A01 | Performance-instrumentering | Aggregerede timing-/tællermålinger for hot paths. | Gemt/deaktiveret; kræver reel, afgrænset instrumentering. Ikke relateret til det tidligere lag, som brugeren har identificeret som eksternt. |
| A02 | Debug logs: App | App-logkategori; ikke et tilsagn om deling. | Gemt/deaktiveret. Implementér runtimefiltrering uden dokumenttekst/credentials. |
| A03 | Debug logs: Panel/AX | Caret-/placering-/adapterdiagnostik. | Windows-ækvivalent er Panel/UI Automation/caret, ikke Apples AX. Gemt/deaktiveret felt findes. |
| A04 | Debug logs: Panel parity | Særskilt paneltilstandsdiagnostik. | Kontrol og separat felt mangler. |
| A05 | Debug logs: WordSuggestorCore | Styrer Core-debug separat. | Gemt/deaktiveret, mens bridge bevidst slår Core-debug fra. Ny sikker diagnostikkontrakt kræves; rå stderr må ikke videresendes. |
| A06 | Ukendt ord-opslag (lokal læring) | Asynkront lookup og lokale aggregerede læringsposter gennem enginegrænsen. | Eksponeres ikke i nuværende bridge. Er ikke samme funktion som at tilføje et brugerord manuelt. |
| A07 | Del ukendt ord-læring (samtykke) | Særskilt samtykke til senere handoff; kræver A06; rå sætninger/tastelog må ikke deles. | Mangler; må ikke forveksles med L02 eller slå skjult netværkstrafik til. |
| A08 | Åbn legacy-indstillinger ... | Åbner ældre tabbet dialog. Indeholder også neurale tale-/runtime-/udtaleværktøjer og udviklingsfunktioner, ikke bare duplikerede synlige rækker. | Q3 vedtaget: åbn eksisterende Windows-indstillinger under migrationen; tilføj ikke automatisk Mac-neuralværktøjerne. |

## Core-, konto- og updater-afhængigheder

### Fælles enginegrænse

Den fungerende JSON v1-bro har kun `suggest` og `addUserWord`, sprogtag, packsti,
tekst før caret, kandidatgrænse og brugerordssti. Den har ikke switches for
forslagskilder, domænelanes, variantfamilier, analyse, punktum-assist eller
læringssignaler. Windows kan derfor ikke levere fuld semantik ved kun at gemme
flere C#-felter eller efterfiltrere resultatlisten efter kilde.

Sprintgrundlaget skal omfatte en afgrænset Core-handoff: additive/versionerede
optioner og capabilities, portabel analyse/fortolkninger, repræsentative fixtures,
samme Core-/packversion og bevis for, at indstillinger påvirker den rigtige
funktion. Bevar den nuværende produktionsdygtige procesbro, timeout/cancellation
og no-echo-krav. Udvid ikke Windows til en separat lingvistisk implementation.

### Konto og sikkerhed

Implementér eksisterende auth-/refresh-/profil-/entitlement-/verifikationskontrakter,
ikke en ny backend eller ændret licensmodel. Windows skal have egen adapter til
sikker credential-/nøglelagring, single-flight refresh, logud/revokering og rolige
offline-/låst-tilstande. Passwords må ikke gemmes. En simpel Hello-success-bool
oven på ubeskyttet refresh-token er ikke tilsvarende Mac-Keychain-beskyttelse.

Microsofts [UserConsentVerifier](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.ui.userconsentverifier)
kan verificere PIN/Windows Hello-biometri og kræver desktop-interop for visning.
Det leverer ikke i sig selv tokenkryptering eller den vedtagne Q1-politik. Den sikre
sessionadapter skal derfor kvalificeres særskilt med rigtig nøglebeskyttelse,
cancel, lockout, manglende hardware og software/RDP-testmiljø.

### Automatiske opdateringer

Windows beholder MSIX/App Installer, signatur-, identity-/publisher-kontrol,
monotone pakkeversioner og den eksisterende canonical beta-URL. Sparkle porteres
ikke. G05 må ikke blot gemme en bool, mens OS fortsat checker/installerer uændret.

Microsofts [AppInstallerManager](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager?view=winrt-22000)
har et Windows 11/API-contract-krav. [SetAutoUpdateSettings](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.setautoupdatesettings?view=winrt-28000)
kræver `packageManagement` og `packageQuery` capabilities. Det er ikke bevis for,
at en almindelig WPF-beta uden nye rettigheder kan bruge den direkte.
Microsoft beskriver også [OS-/PowerShell-administration](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview).

En tidlig teknisk kvalifikation skal derfor kontrollere installeret/unpackaged,
Windows 10/11, almindelig bruger, læsning/ændring/genlæsning af reel OS-præference,
bevaring af feed/identity og offline-/policyfejl. Bevar det eksisterende minimums-OS;
hæv det ikke stiltiende for én kontrol. Hvis samme indstillingssemantik ikke kan
leveres på en understøttet variant, dokumentér en konkret afvigelse til produktvalg
før den erklæres færdig. Den nuværende App Installer-konfiguration har både
OnLaunch og AutomaticBackgroundTask; kontrol, download, installation og genstart
skal holdes adskilt og må ikke afbryde aktiv skrivning.

## Data, migration og testgrundlag

1. Bevar `%LOCALAPPDATA%\WordSuggestor` og eksisterende settings, brugerord, packs,
   editorfaner/kladder og insights. Tilføj eksplicit versionsstyret migration,
   backup, future-schema-beskyttelse og rollback-beskrivelse.
2. Interface-sprog og skrive-sprog er to felter. Stemme-ID'er, lokale genveje,
   caret-tilladelse, wrap og lokale sikkerhedsvalg er ikke universelle Mac-værdier.
3. Respektér `BackendControlPlaneContracts`' opdeling i profilglobal, enhedslokal og
   local-only. Både læringspræferencer og updateren har deres egen autoritet;
   kopier ikke hele Mac-snapshotet til et Windows-JSON-format eller backend-PUT.
4. Bevar false/ukendt samtykke, lokale teksteksempler fra som standard og ingen rå
   dokument-/password-/tokenlogs. Samtykkekontroller må ikke udfyldes fra billederne.
5. Test hver kontrol som kæden UI → settings/adapter → effekt → genstart/persistens.
   Dæk også fejl ved gemning, cancellation, capability-mangel og frakoblet backend.
6. Automatisér kategorier/rækkefølge, alle kontrol-ID'er, hjælpetekster, afhængig
   enabled-state, localization, migration/fremtidige formater og live vs. eksplicit
   gemning. Additiver skal ikke svække de eksisterende 92 app-/Core-checks.
7. Kør rigtig desktop-smoke med mus/keyboard/Narrator, høj kontrast og 100–200 %
   scaling. Kontrollér panel/toolbarfokus, faneskift, TTS, genvejekollisioner og
   understøttede eksterne targets. Release kræver installeret update-/databevaringstest.
8. Fuld funktionsparitet må ikke afsluttes ved at aktivere kontroller uden effekt.
   Midlertidigt utilgængelige adaptere vises ærligt, men tæller som åbne leverancer.

Denne dokumentationsopgave har ikke kørt nye builds, ændret brugerindstillinger,
logget ind eller udført handlinger på backend. Mac-koden er undersøgt statisk;
der er ikke kørt en macOS-app i Windows eller afprøvet usete Mac-dialoger manuelt.

## Afsluttede afklaringer — 2026-11-10

Brugeren har besvaret alle tre spørgsmål og bekræftet kildereferencen. Der er
ingen resterende produktspørgsmål fra analysen. Beslutningerne er registreret som
faktiske svar, ikke som stiltiende accept af anbefalinger.

| ID | Produktvalg | Vedtaget | Status |
|---|---|---|---|
| Q1 | Touch ID-ækvivalent | Windows Hello med PIN eller biometri, med reel sikker sessionadapter. | Afklaret 2026-11-10 |
| Q2 | Tre Windows-modifierfamilier for almindelig/primær/sekundær lane | Ctrl+1…0 / Ctrl+Shift+1…0 / Alt+1…0, valgbare og konfliktkontrollerede. Ctrl+Tab forbliver editorfaneskift. | Afklaret 2026-11-10 |
| Q3 | Legacy-knappens scope | Åbn eksisterende Windows-indstillinger under migration; ikke automatisk et projekt om Mac-neuralruntime. | Afklaret 2026-11-10 |

Det tidligere spørgsmål om nyere kilde er lukket: `ba18b45` indeholder nu de
kontroller, som manglede i den første reference.

[WSA-UX-029](../sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md)
er oprettet med slices 0–9 for baseline/kvalifikation, navigation/legacy,
settings-/datamigration, Core-adaptere, tale, panel, analyse/læring, konto/sikkerhed,
updater og slutkvalifikation. Hele kontrolregisteret har en primær slice og
acceptplan. Teknisk updater-/sikkerhedskvalifikation er tidligt planlagt, så en
konkret platformbegrænsning ikke først opdages ved UI-closeout. Sprintet kan ikke
lukkes som fuld paritet med åbne Core-/backendeffekter.

For brugeren er de få Windows-specifikke produktvalg nu låst. Næste arbejde er
baseline-/platformkvalifikation og derefter venstremenuen med Windows-legacyvisning.
Den nye Settings-UI er endnu ikke implementeret eller slået til.

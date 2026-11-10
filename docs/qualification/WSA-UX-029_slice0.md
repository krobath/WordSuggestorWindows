# WSA-UX-029 slice 0 — baseline og platformgrænser

Dato: 2026-11-10. Platform/ejer: Windows. Kvalifikationsværktøjer implementeret;
baseline-evidens registreres separat fra positiv Hello-/updaterkvalifikation.
Der er ingen ændring af produktets Settings-UI i denne slice.

Samlet baseline-run: **PASS, ti gates**, inklusive frisk kildeeksport. Se
[den maskinlæsbare evidens](WSA-UX-029_slice0_evidence.json) for inputfingerprints,
exitkoder, tællinger, native begrænsninger og fixture-oprydning.

## Leverance og bevisgrænse

Implementeret maskinlæsbart kontrolregister, reproducerbar gate-runner,
arbejdsmappefingerprints, native read-only capability-prober samt en snævert
afgrænset OS-update-test med ændring/genlæsning/gendannelse. Testværktøjerne åbner
ikke Hello-enrollment og ændrer ikke produktionspakken eller offentlige feeds.

**Hello og styring af OS-opdateringer er ikke positivt kvalificeret.** Ingen
prototype må aktivere de nye produktkontroller på baggrund af API-tilstedeværelse,
mocktests eller baseline PASS. Slice 1's layout kan bygges uafhængigt; slice 7/8
beholder deres positive integrationsgates. Slice 0 må ikke kaldes fuldt godkendt
platformskvalifikation, mens disse beviskrav er åbne.

## Fastlåste input

- Kanonisk Mac: `ba18b451372dd2e3f125e557c5db45894f4e1ae3`.
- Windows-gitbase: `71c342edd7b0b2aa89ced5ffc6b9f36e1332dbfd` med eksisterende
  **uncommitted** editor-/releasearbejde. Baseline er kvalificeret mod denne
  arbejdsmappe, ikke mod en påstået ren HEAD eller offentlig RC.
- Core: `5d8d7a0fd578a8373ce17f79a25e78a1197692c1`, ren arbejdsmappe og samme
  revision som `dependencies.lock.json`.
- DA-pack SHA-256:
  `6970c398077c235355f513d9491e4193534fe88b751aeea07eddef9abc3fb82f`.
- .NET SDK `10.0.403`; host Windows x64 build `10.0.26300.0`, almindelig bruger.
- Kategorier og alle 72 kontrolposter er i
  [settings-controls.v1.json](settings-controls.v1.json), med canonical source,
  primary slice og eksplicit `not_run_in_sprint` for fremtidige runtimeeffekter.
  Registret kopierer ikke brugerens screenshotværdier eller samtykke.

Runneren gemmer HEAD, dirty-status og relative fil-/SHA-256-poster i ignored
`.artifacts/settings-qualification/<run>/source.json`; `baseline.json` har separate
app-/C#-testfingerprints, Core-/packbinding, gate-exitkoder og varighed. Kildeindhold,
credentials, profil-/dokumenttekst og private nøgler skrives ikke til manifestet.
App-/testkilden skal være uændret mellem start og slut. Underliggende eksisterende
kodeændringer indgår ikke stiltiende i slice-0-committet.

## Automatiseret baseline

Fra Windows-repoet:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_settings_slice0.ps1 -Desktop -CleanSource
```

Uden `-Desktop` køres ikke tests, der flytter fokus eller afspiller lyd.
`-CleanSource` genbygger fra frisk working-tree-eksport og pinned Core, ikke fra
genbrugte buildmapper. Sourceeksporten kræver eksisterende toolchain og initial
dependency-download. Det er ikke bevis for installation af toolchain på en ny VM.

| Gate | Observeret |
|---|---|
| `test_settings_qualification.ps1` | PASS, 35 kontrakt-/sikkerhedschecks; mock OS-tests er ikke native adgangsbevis |
| `build_app.ps1` | PASS, 0 warnings/errors |
| `test_core_cli.ps1 -SkipBootstrap` | PASS, seks reelle DA batch/JSON-fixtures |
| `test_app.ps1 -SkipBuild` | PASS, 92 checks inklusive Core, packs, persistens, timeout/cancel/reap, faner og stale/duplicate commits |
| `test_run_scripts.ps1` | PASS, ni checks på egen usynlig fixture, ikke brugerens app |
| `test_release_scripts.ps1` | PASS, 43 syntetiske release-/transport-/backendkontraktchecks; ingen publicering eller SSH-upload |
| `test_gui.ps1 -SkipBuild` | PASS, 42 checks på rigtig desktop, Notepad 11.2607.14.0, én skærm/96 DPI; keyboard/mouse, fokus, filer, Unicode, faner og recovery |
| `test_media.ps1 -SkipBuild` | PASS, tre adapterchecks; real OCR og OneCore Helle-proces, STT-wire. Ingen human lyd-/mikrofon-/snipping-sign-off |
| `test_settings_platform.ps1` | Probe gennemført som almindelig bruger; negative/ikke-kvalificerede resultater nedenfor |
| `test_clean_checkout.ps1 -IncludeWorkingTree` | PASS: frisk app/SQLite/Core-build, seks Core-fixtures og 92 appchecks uden kopierede buildmapper. Core har eksisterende pkg-config/Sendable-advarsler; app-build har 0 warnings/errors |

Slutrun ligger i ignored `.artifacts/settings-qualification/slice0-baseline-final`.
Det har ti vellykkede exitkoder og afsluttende kontrol af uændrede app-/testkilder.
Ren working-tree-eksport er ikke det samme som ren committed HEAD: tidligere
editor-/releasekode er bevidst stadig uden for dette commits scope.
De tidligere launch-/release-/clean-export-gates er en del af dette registrerede
arbejdsmappeinput og kan også være uncommitted. Fuld baseline-replay kræver derfor
det samme input/snapshot eller efterfølgende særskilt integrerede afhængigheder.
Et rent checkout af kun slice-0-committet er ikke den appbaseline, som er attesteret
her. De nye kontraktchecks og read-only platformproben er selvstændige.

## Windows Hello: observeret stop før enrollment

`UserConsentVerifier.CheckAvailabilityAsync()` returnerer `DeviceNotPresent`.
`KeyCredentialManager.IsSupportedAsync()` returnerer false. Begge kald blev
afviklet uden fejl og uden login-, PIN- eller enrollment-dialog. Derfor er der
ikke kørt user-verification eller beskyttet-secret/unseal-/restart-roundtrip.

Det betyder i produktet: bevar den vedtagne PIN-/biometripolitik som mål, men vis
ikke en fungerende Hello-session på denne baggrund. DPAPI alene eller en
UserConsentVerifier-success-bool er ikke bevis for Hello-beskyttelse af et
refresh-token. En senere sikker sessionadapter skal demonstrere faktisk
nøgle-/secretbeskyttelse og ingen stille fallback til ubeskyttet tokenlagring.

Positiv videre kvalifikation kræver en Hello-provisioneret Windows-testsession
og den konkrete sikre adapter, med PIN, tilgængelig biometri, cancel, lockout,
genstart og ændret tilgængelighed. Provisionering udføres lokalt af ejeren; PIN,
passwords og private nøgler må ikke sendes i chat eller lægges i repo.

## OS-update: API findes, ændring fejler

Der blev via det **eksisterende** `package_app.ps1 -LocalTestIdentity`-flow bygget
og SignTool-verificeret en separat testpakke `0.2.0.10` med unik LocalTest-identity
og to-dages ikke-eksporterbar testnøgle. Den blev installeret fra et privat lokalt
App Installer-feed. Ingen produktionsidentitet, pakkeversion eller offentlig URL
blev ændret. Den nødvendige midlertidige testtillid blev kun givet til en ikke-CA
codesigning-leaf; ingen trust root blev tilføjet.

Den første fixturebygging blev afvist, fordi SignTool blev kørt før midlertidig
testtillid. Den nøgle blev slettet, uden installation. Efter korrekt rækkefølge
bestod signatur-/installationsgaten. Produktets releasekode blev ikke ændret.

På den installerede testpakke var OnLaunch og AutomaticBackgroundTask true.
Get-cmdletten kunne læse tilstanden som almindelig bruger. Følgende blev derefter
forsøgt og genlæst, med identity-/publisher-/konfigurationskontrol:

| Vej | Resultat | Tilstand efter forsøg |
|---|---|---|
| `Set-AppxPackageAutoUpdateSettings`, både minimal og komplet parameterform | Blokeret, HRESULT `0x800706F7` | Verificeret uændret |
| `AppInstallerManager.GetDefault().SetAutoUpdateSettings` | Blokeret, samme HRESULT | Verificeret uændret |
| Oprydning | PASS | Testpakke, privat testnøgle og midlertidig leaf-tillid fjernet; fravær kontrolleret |

Dette er et faktisk resultat for **unpackaged PowerShell, build 26300 og lokal
fil-feed-fixture**. Det beviser hverken en fejl på alle Windows-versioner eller,
at årsagen er capabilities/permissions. En pakket WPF-proces' autoritet er ikke
testet. Windows 10, stabil Windows 11, et særskilt HTTPS-testfeed og positivt
ændr/genlæs/gendan-flow er fortsat åbne kvalifikationspunkter. Der er ikke lavet
en falsk lokal bool til G05 eller indført et alternativt updaterflow.

## Reproducerbar native probe

Read-only hostprobe uden testinstallation:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_settings_platform.ps1
```

På en allerede installeret **isoleret** WordSuggestor LocalTest-MSIX kan begge
OS-veje prøves med `-TestPackageFamilyName <fixture-family>`. Scriptet kræver
matchende `WordSuggestor.Windows.LocalTest.<hex>`-navn og tilsvarende testpublisher,
afviser produktionspakker/elevation/ukendt flagformat og forsøger gendannelse i
finally også efter delvis Set-fejl. Pausede pakker ændres ikke. Fejl gemmes som
type/HRESULT, ikke rå fejltekst, URI'er eller credentials. For volatile
`LastCheckedForUpdates` kræves ikke bitidentisk genlæsning; øvrig konfiguration
og begge kontrolflag kontrolleres. Fixtureinstallationen/trustforløbet er et
separat testmiljøtrin, ikke noget read-only hostproben udfører automatisk.

## Fejl fundet i orkestrering og test

Den første samlede runner stoppede ved redirected native stderr før exitkoden.
Det er rettet i `Invoke-SettingsGateProcess`: stderr optages som evidens, og
processens exitkode bestemmer gaten. Reelle fixtures tester stderr+exit 0,
bevaret caller-fejlpolitik og nonzero exit 9. Der er ikke svækket nogen app-,
Core- eller releaseforventning for at få PASS.

## Videre arbejde

Gå videre til slice 1 med native venstremenu, hjælperækker og den hidtidige
Windows-settingsvisning som legacy. Hold Hello og styrbar automatisk update
ukvalificeret, indtil de respektive positive beviser foreligger. Hvis en konkret
understøttet OS-variant kræver ændret produktsemantik, forelægges netop dette;
minimums-Windows-version og de tre vedtagne produktvalg er ikke ændret her.

Windows ejer denne evidens. Core, backend og Mac er ikke ændret af slice 0;
fælles docs får kun konsolideringshenvisninger ved en senere separat opgave.

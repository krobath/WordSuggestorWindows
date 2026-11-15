# Prompt til macOS-/backend-agenten: Windows EXE-beta

Implementér en kompatibel udvidelse af den eksisterende beta-distribution i
WordSuggestorBackend. Windows-ejeren har efter brugerens prioritering parkeret
G05/App Installer-fejlsøgning og implementeret en almindelig per-user EXE-installer
med tilbud om opdatering, som brugeren selv godkender.

Læs først AGENTS.md, lokale dokumentationsstandarder og backendens aktuelle kode.
Baseline: `2f67ddc6f5382b286e3039677a05677fe4d7d16f`,
`docs/60_operations/O-052_Multi_Platform_Beta_Distribution.md` og eksisterende
manifesteksempler. Kontrollér nyere commits; bevar uvedkommende ændringer.

Windows-implementationens kilde er `WordSuggestorWindows/docs/ExeBetaRelease.md`
og `Services/WindowsExeUpdateService.cs`. Windows-repoet ejer app, installer,
signering, Windows-tests og PowerShell-uploader. Du ejer backendens kontrakt,
katalog, serverpublisher, nginx og /beta-side. Ændr ikke Windows- eller macOS-appens
kode uden en særskilt aftale. Dokumentér kontrakten i backendens egne docs.

## Resultat brugeren skal opleve

På https://wordsuggestor.com/beta kan en Windows x64-bruger hente en signeret
EXE, installere uden normal administratoradgang og senere få et tilbud i appen,
når en nyere beta findes. Appen åbner først installeren efter brugerens accept
og kontrol af størrelse, hash, trusted Authenticode og stabil publisher.
Mac-download, Sparkle og eksisterende MSIX skal fortsat fungere uændret.

## Udvid eksisterende flow

1. Understøt `platform=windows`, `architecture=x64`, `channel=beta`,
   `package_type=exe` i det eksisterende katalog, manifest og publisher.
   Bevar de hidtidige MSIX- og macOS-varianter. Vælg EXE som den almindelige
   Windows-beta-download; undgå tvetydige dubletter i kataloget.
2. Definér den autoritative manifestudvidelse og et valideret eksempel. Den skal
   indeholde produkt-ID `WordSuggestor.Windows.Beta.Direct`, displayversion,
   numerisk fireleddet package_version, buildnummer, minimumskrav, release notes,
   kendte begrænsninger, pending/active-status, EXE og JSON-feedets navn/størrelse/
   SHA-256 samt signeringens eksakte publisher/identity, verificeringsresultat,
   tidspunkt og værktøj. Bevar princippet om Windows-publisherens autoriserede
   signeringsattestation; Linux må ikke foregive at verificere Windows trust.
   Afvis preview-ID'er/filnavne, unsigned/test-signering og uverificeret aktiv release.
3. Genbrug den eksisterende SSH-staging og server validate/register/publish.
   Dokumentér præcis, hvilke tre filer Windows skal uploade: manifest, versioneret
   EXE og stabilt JSON-feed. Friske staging-ID'er, `.uploading` til atomisk rename,
   strict SSH host-key-pinning og eksisterende fejlkoder/struktureret JSON består.
   Ingen direkte offentlig upload eller ny parallel publiceringsservice.
4. Publicér versioneret pakke først, stabilt feed dernæst, katalog sidst.
   Pending må ikke give offentlig download; active kræver validerede metadata.
   Dokumentér withdrawal/feed-stop, samtidige publishers og rollback med større
   numerisk version. Identisk version med ændrede bytes må aldrig overskrives.
5. Tilpas nginx/content-type og /beta-links til EXE og JSON. HTTPS med normalt
   betroet certifikat. Bevar macOS/Sparkle og MSIX/App Installer-URL'er.

## Windows-klientens præcise feedformat

Canonical feed:
`https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta.update.json`

Versioneret EXE:
`https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta_<package_version>_x64.exe`

UTF-8 JSON med præcis ti felter, ingen dubletter eller ekstra felter:

```json
{
  "schema_version": 1,
  "platform": "windows",
  "architecture": "x64",
  "channel": "beta",
  "package_version": "0.2.0.3",
  "version": "0.2.0-beta.3",
  "filename": "WordSuggestor.Beta_0.2.0.3_x64.exe",
  "size_bytes": 123456,
  "sha256": "0000000000000000000000000000000000000000000000000000000000000000",
  "publisher": "CN=REPLACE_WITH_APPROVED_PRODUCTION_SUBJECT"
}
```

Erstat fixtureværdier med reelle bytes/hash/subject. Fire versionstal 0..65535
uden overflødige foranstillede nuller; positiv størrelse højst 1 GiB; lowercase
SHA-256 hex64; filnavn afledes præcist af versionen. Publisher skal matche det
godkendte Windows-certifikats eksakte subject DN. Maksimal feedstørrelse 32 KiB.
Ingen servervalgte URL'er, credentials, dokumenttekst eller shellargumenter.
404 betyder roligt ingen offentlig EXE-release endnu. Serveren må ikke redirecte
feed/download; klienten følger ikke redirects. Feed er offentlig metadata uden
login, cookies eller individuel brugsregistrering.

Windows-preview bygger foreløbig kun `release-manifest.draft.json` med
`backend_contract_status=extension_required`; den er **ikke** en godkendt backend-
kontrakt og må ikke gøres publicerbar ved at svække validering. Lever en præcis
udvidelse, så Windows-ejeren efterfølgende kan opdatere sin uploader i det
eksisterende flow. Hvis en reel uforenelighed findes, dokumentér den før ændring.

## Test og aflevering

Test schema/filnavn/hash/size/version/signer/publisher, preview-afvisning,
pending uden download, atomisk active, withdrawal, rollback, staging-kollisioner,
samt regressions for DMG/Sparkle og MSIX. Ingen produktionspublicering før de
reelle Windows signerings- og installationsgates er godkendt.

Lever backendcommit, opdateret O-052/manifesteksempel, præcise exit codes,
upload/publish-kommandoer og deploy-anvisning efter den eksisterende macOS-
deployprocedure. Send en kort prompt retur til Windows-ejeren med den endelige
kontrakt og referencer. Commit kun hvis brugerens gældende autorisation tillader
det; ingen secrets eller genererede produktionsartefakter i git.

# WSA-RT-017_windows_beta_publication_tooling

Date: 2026-11-12. Third coherent cleanup delivery after developer restart and tabs.

## Summary

Review, harden and commit the previously retained Windows beta/release tooling.
Retain WPF, portable Core, existing pack format and MSIX/App Installer. The
authoritative O-052 server contract at backend commit 2f67ddc remains unchanged;
Windows owns local packaging/signing/qualification and private staging only.

## User-visible behavior

Developers can build a self-contained x64 payload/unsigned MSIX without secrets.
Approved trusted signing produces a bundle for pending registration and later
qualified activation on the existing beta page/feed. Production remains gated.
No payment/licensing changes. Installed copies use their bundled Core. Native test
identities/data are separate; ordinary user settings/drafts/packs remain in place.

## Implementation details

Separate display/build/four-part MSIX versions; stable beta identity/publisher;
SignTool plus Windows trust verification and signer attestation; canonical HTTPS
App Installer schema/policy and exact three-file manifest/hash/size validation.
Reject unexpected/duplicate XML policies. Pin the original backend validator bytes
before executing its portable AST functions; no silent missing-reference pass.

The uploader uses a dedicated strict ED25519 known_hosts entry, bounded native
process arguments/output/deadlines and fresh UUID private upload/atomic rename.
Server JSON/exit codes and target identity are verified. RegisterPending cannot
activate; Publish rechecks native qualification, monotonic public version, actual
payload and pre/post Mac beta URLs/appcast. The server retains package-first,
feed-second, catalog-last ownership. Backend checkout path is required operator
configuration, not a machine-specific default. WhatIf contacts no network.

The installation harness requires explicit temporary-machine-trust opt-in for
isolated local tests. Test signer/publisher share a nonce; only a non-CA code-sign
leaf is temporarily trusted, never a root. Existing installations/nonempty test
data are preserved, and cleanup removes only packages actually installed by the
current run. Qualification records contain error categories/HRESULT only. Explicit
installed probes cannot overwrite settings/documents/existing results. Source-only
clean-checkout export supports working-tree candidates and shorter fixture paths.

## Files changed

- /README.md
- /docs/Architecture.md
- /docs/ManualSmoke.md
- /docs/ParityMatrix.md
- /docs/Plan.md
- /docs/README.md
- /docs/Release.md
- /docs/changes/2026-11-12_wsa_rt_017_beta_publication_tooling.md
- /docs/qualification/WSA-RT-017_release_cleanup.md
- /scripts/local_test_trust.ps1
- /scripts/package_app.ps1
- /scripts/publish_beta_release.ps1
- /scripts/release_common.ps1
- /scripts/release_transport.ps1
- /scripts/test_clean_checkout.ps1
- /scripts/test_gui.ps1
- /scripts/test_msix.ps1
- /scripts/test_release_candidate.ps1
- /scripts/test_release_scripts.ps1
- /src/WordSuggestorWindows.App/App.xaml.cs
- /src/WordSuggestorWindows.App/Services/ReleaseQualificationProbe.cs
- /src/WordSuggestorWindows.App/Services/WindowsDataPaths.cs
- /src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs
- /tests/WordSuggestorWindows.Smoke/GuiSmoke.cs
- /tests/WordSuggestorWindows.Smoke/PackageActivation.cs
- /tests/WordSuggestorWindows.Smoke/Program.cs
- /tests/verify_backend_manifest.py

## Feature flags / settings impact

No new product schema/consent or automatic-update toggle authority. Settings
remains schema 6, editor sessions schema 1, existing secure credentials unchanged.
Developer release parameters/probe flags are explicit. Default MSIX qualification
exits 3 before certificate/package changes; native trust tests remain opt-in.

## Logging / telemetry impact

No new telemetry. Manifests/build identity contain hashes/version/public signer
metadata only. Uploader suppresses raw remote errors and never prints private keys
or credentials. QA uses synthetic text; full logs/artifacts stay ignored. Probe
results use reserved diagnostics names and contain no user text.

## Validation performed

See [commands, exact source, measured results and matrix](../qualification/WSA-RT-017_release_cleanup.md).
No production signer, trust mutation, native installation/update, real SSH upload,
pending registration or public activation is claimed. All software/payload results
are distinguished from those external/native gates. Git diff/source ownership,
secret patterns, binary artifacts and unintended paths are reviewed before commit.

## Known limitations / follow-up

Provision approved production signing and operator SSH configuration securely, then
run disposable native N-to-N+1 and remaining hardware/human acceptance. In practice
this lets users install/update from /beta while preserving drafts/settings/packs;
until those gates pass, public activation stays blocked. Hello remains user-deferred.
Notepad is the only external app with current end-to-end evidence. No push occurs.

## Consolidation notes for `WordSuggestorDocs`

Windows live docs stay here; shared Docs may link the runbook and evidence later.
No changes to macOS, Core, language packs, backend or shared Docs. Historical
November 8 packaging statements are superseded by the current runbook's evidence
boundaries and O-052 canonical paths, not retroactively treated as qualification.

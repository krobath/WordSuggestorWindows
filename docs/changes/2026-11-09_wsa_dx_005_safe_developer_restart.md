# WSA-DX-005_safe_developer_restart

Initial implementation: 2026-11-09. Reviewed and qualified: 2026-11-12.

## Summary

Prevent MSB3027/MSB3021 when a running development toolbar owns the build output.
Retain the WPF implementation and its own save/cancel behavior.

## User-visible behavior

From the workspace parent, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBootstrap -Restart
```

The script asks the matching app to close normally and waits up to 120 seconds.
Cancel or an unanswered save prompt aborts rebuilding and preserves the app.
Without `-Restart`, run/build explain a locked output before bootstrap/MSBuild.
Closing the editor alone leaves the toolbar running. Use **Afslut WordSuggestor**
to exit manually. `build_app.ps1 -ArtifactsPath <directory>` builds separately.

## Implementation details

`app_process.ps1` compares absolute executable paths, PID and process start time,
then posts WM_CLOSE only to that process's WordSuggestor toolbar. This works for
tool windows with no Process.MainWindowHandle. Protected or changed targets fail
safely. Relative paths resolve from PowerShell's filesystem location rather than
the CLR working directory. Launcher startup text is inherited only by the child;
the caller's environment is restored in finally. No process termination is used.

## Files changed

- /scripts/app_process.ps1
- /scripts/build_app.ps1
- /scripts/run_app.ps1
- /scripts/test_run_scripts.ps1
- /README.md
- /docs/ManualSmoke.md
- /docs/Plan.md
- /docs/README.md
- /docs/changes/2026-11-09_wsa_dx_005_safe_developer_restart.md

## Feature flags / settings impact

Developer-only `-Restart`, `-ArtifactsPath` and `-AppPath`; no persisted settings
or data migration. Save decisions remain in the app.

## Logging / telemetry impact

Launcher output excludes supplied sample text. No telemetry or new product logs.
Test strings are synthetic; test artifacts remain ignored.

## Validation performed

`powershell -ExecutionPolicy Bypass -File scripts\test_run_scripts.ps1`: PASS 14
checks on 2026-11-12. A hidden, owned WinForms fixture verifies exact/relative
paths, wrong-target rejection before any close message, cancellation preserving
the live process, graceful exit, child-only text, caller environment restoration
and script parsing. No user WordSuggestor instance was closed. Independent build
validation is recorded in the later editor/release closeout.

## Known limitations / follow-up

This does not automatically resolve save prompts: the user chooses save/discard
or cancel in the app. It never closes installed copies or a different executable
path merely because the process name matches. Normal shutdown can exceed the
bounded wait; rerun after resolving the prompt. No push or publication.

## Consolidation notes for `WordSuggestorDocs`

Windows developer behavior belongs in this repository. Shared documentation may
link this note; macOS, Core and backend contracts are unchanged. The next delivery
records the existing editor tabs and recovery work so developers can use multiple
documents and see its verification limits separately from release tooling.

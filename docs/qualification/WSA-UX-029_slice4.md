# WSA-UX-029 slice 4 — speech rate, typing strategy and highlights

Date: 2026-11-10. Windows x64 `10.0.26300.0`, ordinary user, one monitor at
96 DPI. .NET SDK 10.0.403. Windows base `c9ec090`; unchanged Core
`c87f68aa2c145220817c0264279854d5b9b32078`; Mac reference `ba18b45`.
DA SHA-256 `6970C398077C235355F513D9491E4193534FE88B751AEEA07EDDEF9ABC3FB82F`.

## Scope

Retain WPF, OneCore, native SAPI, Core and existing pack/data/commit boundaries.
O01–O05 now have effective voice/tempo/strategy/highlight behavior. Native metadata
and a non-selecting adorner replace estimated/moving-selection highlighting.
Candidate audio shares the owned cancellable service. Schema 3 upgrades old speed
requests once and creates original-version backups. No backend, new consent,
language/ranking rule, signing, package/feed or other-platform change is included.
See [speech contract and recovery](../Speech.md).

## Reproduce

From a Windows checkout with the locked sibling Core and installed toolchains:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_qualification.ps1
```

Use Settings `-Mode Speech` for speed/trigger/migration/process/native playback,
`-Mode SpeechUi` for actual-window/physical-input testing. All includes these and
the existing Core/State/Ui gates. Tests create isolated synthetic profiles, play
synthetic audio and open their own Notepad document; an unlocked desktop and
installed Windows voices are required. Do not interact during GUI tests. No
production account, trust installation or signing secret is needed. Tool sandbox
access is distinct from the Windows process, which remains an ordinary user.

## Results

Windows and the locked Core source were exported with `git archive` into empty
sibling directories. No working-tree sources, generated binaries or build caches
were copied. Installed toolchains were prerequisites. Existing scripts above ran
against the export; app/CLI output paths and existence inside it were checked.

| Gate | Final result |
|---|---|
| Fresh Windows build | PASS, zero warnings/errors |
| Fresh SQLite/Core bootstrap | PASS, CLI produced inside the isolated export |
| Existing Danish fixtures | PASS, six real batch/v1 ordered parity fixtures |
| Portable Core contract | PASS, 169 checks against the unchanged locked Core/pack |
| Speech adapter/native | PASS, 102 checks: all percentage steps, migration/backups, trigger boundaries, no duplicate/paste/cross-document trigger, replacement/timeout/reap/privacy/cleanup, real PCM timing, native word/sentence/none and original Unicode/entity offsets on both routes |
| Windows Core adapter | PASS, 58 checks |
| Settings state / interactive UI | PASS, 63 / 58 checks |
| Speech interactive UI | PASS, 24 checks: physical editor/Notepad input, four strategies, continued typing, real markers, preserved caret/selection, manual stop/navigation/edit/focus cancel, native percentage range and live voice/system-mode persistence |
| Standard app | PASS, 67 checks from the committed-source scope |
| App/editor/Notepad GUI | PASS, 30 checks at 96 DPI, one monitor |
| OCR/STT/TTS media | PASS, three existing adapter checks; no human acoustic claim |
| Qualification safety | PASS, 35 contract/mock checks; not positive Hello/OS evidence |

Qualified candidate tree: `74461e97fc867f294a390b89ed33e87e9f179102`.
Final documentation is added after execution; these source/test/script/lock hashes
are verified unchanged before commit:

- `src`: `5f3e29e575c5285e860176798dd51eb00e65cbbe`
- `tests`: `94e18411834988d92fe5ce845fcbb1c7c29d95d5`
- `scripts`: `e21d8f6418e934139fe299b21682f124be6b6247`
- `dependencies.lock.json`: `101c12c5569d913922ed17efbc5ffb9a5f96f5ba`

Measured PCM durations for the same synthetic sentence (not a latency/quality
benchmark; voices may round requested rates):

| Native backend | 50% | 100% | 150% |
|---|---:|---:|---:|
| OneCore | 7535 ms | 3800 ms | 2550 ms |
| SAPI Desktop | 7510 ms | 3874 ms | 2509 ms |

The additional `😀 æble å & <ord>.` fixture requires exact original offsets for
æble, decomposed å and ord on both routes. It exposed SAPI's unsafe CharacterCount
semantics with entities; the corrected single-text-node projection passes without
normalizing text or relaxing expectations. OneCore uses its original inclusive
start/end indices. Earlier intermediate runs are not counted as final qualification.

Developer inspection of synthetic Settings renders confirmed the sidebar/control
layout and found a clipped system-tempo label at minimum size. The label now wraps;
the native slider and its automation range use 50–150 percentage units. This is
limited developer visual evidence, not human accessibility or audio sign-off.

For clean-source reproduction, export the finished Windows commit and its locked
Core revision to empty sibling directories. Keep installed toolchains on PATH,
invoke the exported scripts by absolute paths and verify output locations in the
export. Bootstrap Core on the first run; do not copy existing build outputs.

The earlier working-tree Settings regression passed: native speech 98, Core adapter 58,
state 63, Settings desktop 58 and speech desktop 25 checks. The latter includes
one actual tab-switch check for the earlier uncommitted tab implementation.
The final four Unicode/entity checks are included in the 102-check export above.
Working-tree evidence is separate from the committed-source qualification; unrelated editor/release
work is preserved outside this slice. Actual cue/selection tests exposed and
fixed a former selection-collapse issue; expectations were not weakened.
The ordinary local Debug output was rebuilt at the end with zero warnings/errors;
its broader working-tree app suite also passes 92 checks. Those extra editor/tab
checks do not silently enter this commit. No user app instance was force-closed.

Core implementation/pack are unchanged. The complete Swift suite was not rerun;
slice 3 records its compilation blocker in unchanged Windows test/tool files
(`CFAbsoluteTimeGetCurrent`). Targeted CLI/adapter regression is run here, not
relabelled as a full Swift-suite PASS. Mac fixture execution is not performed.

## Manual smoke matrix

| Area | Automated/developer evidence | Remaining human/hardware qualification |
|---|---|---|
| O01 voice | Real OneCore/SAPI playback, exact-locale ordering and per-language save; missing saved IDs retained by existing Settings tests | Missing-voice installation/removal on dedicated profile; listen to each supported language/voice and fallback |
| O02 OS links | Existing fixed Windows speech/language URI adapters retained | Actual click-through on Windows 10 and Windows 11, adding a voice in OS Settings |
| O03 tempo | All 21 percentage values, migration/idempotence, live keyboard save and real PCM durations at 50/100/150 on both native routes | Human listening at intermediate steps and voice-specific rounding; old SAPI integer tempo is not acoustically identical |
| O04 strategies | Physical word/sentence/paragraph triggers and None; paste/duplicate/different-document rejection; next-word typing retains committed audio without queueing | IME/dead keys, abbreviation/hyphen differences vs Apple segmentation; long typing session with human feedback |
| O05 highlight | Actual native cues, separate adorner, unchanged selection/caret/text, manual stop and navigation/edit cancel; sentence/none modes on both routes | Narrator/high contrast and mixed 125/150/200% DPI/multiple monitors; human audio-to-highlight judgment |
| Focus/document | Real editor plus Notepad, foreground verification, no external import/highlight; unsupported focus cancellation; working-tree tab cancellation tested separately | Committed candidate has no earlier uncommitted tab feature; hardware/UI flows across all target applications remain open |
| Process/privacy | Replacement identity, timeout/cancel/reap, invalid metadata, stderr flood/privacy and owned scratch cleanup | Hard-crash orphan-audio recovery; AppLocker-restricted and Windows 10 sessions |
| Settings/data | V1/V2 migration, exact backups, false/unknown preservation and restart; primary explicit override vs legacy system mode | Signed N→N+1/known-good higher-version rollback and uninstall data retention |
| Existing speech/media | Candidate audio routed through same service; OCR/STT regression retained | Audible TTS quality, microphone recognition and screen snip remain human gates |
| Account/update | Existing unavailable/status behavior retained | Positive Hello/OS-toggle and official signed-release gates remain open |

Automated Windows desktop observation is not full human/manual sign-off.
No production release/upload/install/update is performed. No human listening,
screen-reader, Windows 10, physical mixed-DPI or complete Mac segmentation parity
is claimed by this slice.

## Recovery and next product step

Use the [schema-3 recovery procedure](../Speech.md#schema-3-migration-and-rollback).
Normal data locations and backups remain; do not run old binaries as writers of
newer data. The retired unowned candidate-audio process is replaced, not silently
removed functionality. Native failure is a calm unavailable/error state.

Next slice 5 makes suggestion settings actually select sources/domain lists,
adjust responsiveness/details/fade and configure three conflict-checked shortcut
lanes. Hover previews will use this cancellation service. Analysis/local learning,
accounts/Hello and OS updater authority remain subsequent work.

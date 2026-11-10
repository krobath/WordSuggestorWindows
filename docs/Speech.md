# Windows speech contract and recovery

Updated 2026-11-10, WSA-UX-029 slice 4. Windows-owned implementation guide;
canonical product reference is macOS `ba18b45` (`SpeechSpeedPolicy`,
`TypingReadAloudDetector`, `AppState` and Settings copy). WPF and offline Windows
OneCore/SAPI are retained; no Core/backend/pack format change is made.

## User controls O01–O05

| Control | Windows behavior |
|---|---|
| Voice | Compatible locale voices first, exact locale before source preference, OneCore then name; explicit override per writing language. Missing saved IDs survive unrelated saves. No matching voice exposes a declared fallback, not fictional language support. |
| System settings | Existing `ms-settings:speech` and `ms-settings:regionlanguage` links; installation/availability belongs to Windows. |
| Speed | Native slider/automation range 50–150%, 5% steps, 100% neutral. Persist normalized delta -0.5…0.5. Native OneCore receives multiplier 1+delta; native SAPI receives escaped SSML percentage of its base rate. |
| Typing strategy | None, word, sentence, paragraph. Separate from manual selection/document speech and independent of highlight mode. |
| Highlight | None, word or sentence; actual native timing, original UTF-16 ranges and a separate adorner. Selection, caret, focus, document text and undo are not used as a moving marker. |

The previous Windows system-tempo and speech-language controls remain in legacy
settings, with their values preserved. System-tempo mode does not apply the saved
app multiplier. The primary view labels it as system tempo plus saved app tempo;
the visible notice explains how to choose app control. Explicit primary voice or
speed changes turn system-tempo mode off as part of the same live settings write.
Failure rolls back both fields. Opening Settings or changing unrelated controls
does not change that intention. Legacy retains its explicit Save/Cancel and
disables its custom controls while system mode is selected.

As in the old Windows adapter, voice IDs remain selected per language even in
system-tempo mode. The native tempo default is used in that mode. The legacy
auto-detect speech-language value still resolves to the selected writing language;
new language detection/ranking rules are not added to C#.

## Typing, targets and manual speech

Observe confirmed RichTextBox/native text snapshots, not global keystrokes.
The first snapshot establishes a baseline. A single inserted boundary at the
previous caret triggers reading; selections, replacements, pasted blocks and
target/document changes reset safely rather than replaying text.

- Word: space, tab, CR/LF/CRLF, `. ! ? , ; :` after a completed Unicode word.
- Sentence: specifically a full stop followed by a space (`. `).
- Paragraph: specifically a full stop followed by CR/LF/CRLF; read the last paragraph.
- None: no automatic reading. Legacy page/all values migrate to paragraph.

The explicit trigger rules follow the reference. Windows presentation spans use
bounded Unicode words and punctuation-delimited sentences; they are not Core
morphological readings or grammatical inference. Abbreviation/hyphen boundary
details can differ from Apple's native substring segmentation and need a Mac-side
comparison before claiming identical segmentation for every language.

Automatic speech retains a committed span while the user appends the next word.
A new completed boundary replaces the previous utterance; there is no growing
speech queue. Editing its earlier prefix, navigation/selection changes, settings
changes or loss of target focus cancels safely. Manual speech stops on editing or
navigation. Document reference/generation prevents old cues from coloring another
buffer, even if two documents contain identical text. Candidate audio shares the
same selected voice/tempo and cancellation service; hover scheduling is slice 5.

External automatic reading uses the existing verified Notepad native adapter and
enabled global-capture setting. Target identity includes HWND/control/PID/process
start. Recheck foreground before starting; an unsupported/secure target or focus
change cancels on the existing capture observation. No external highlight,
clipboard operation, document import or text injection is performed. Word/Outlook/
browser automatic typing speech is not qualified. Existing explicit selection
import/manual speech remains separate.

## Worker boundary, timing and privacy

`WindowsTextToSpeechService` owns one process and opaque identity per utterance.
`Speech/WindowsSpeechWorker.ps1` is a fixed embedded resource. Launch the Windows
system PowerShell path directly; pass only fixed arguments and the owned script
path. Version-1 JSON on stdin carries text/options and the parent-owned WAV path.
Text is limited to 16,000 UTF-16 units; the utterance watchdog is 20 minutes.
No text-bearing command, script interpolation, JSON temp payload or raw stderr
is used. Stdout is bounded to 2 MiB, individual lines 512 chars, stderr 256 KiB.
Excess output/failure/timeout cancels and reaps the process.

The private response is numeric `WS_TTS|CUE|startMs|durationMs|start|length`,
`WS_TTS|AUDIO|durationMs`, and playback start/end/error markers. Unknown lines
are discarded. Cues must fit the original buffer and not split a surrogate pair.
OneCore uses original input start/end positions (inclusive end); SAPI uses
System.Speech synthesis events. Our SSML contains one escaped text node: subtract
the fixed prefix from the native position and use decoded event Text length,
checking exact equality with the original substring. CharacterCount can include
entity markup and is not a safe original-buffer length. These projections retain
emoji, decomposed characters and literal XML punctuation without normalization.
Sentence highlights use native sentence cues on OneCore and map
SAPI word timing to the enclosing Windows presentation sentence. Missing metadata
means no marker, rather than invented estimated progress.

PCM is rendered before playback, with its measured duration reported; timing
starts at native playback start. The temporary directory holds only fixed worker
code and the audio waveform. Completion/cancellation removes exact owned files
and the directory; Stop kills/reaps the process tree first. A hard OS/process crash
can leave an audio file in the user's temp directory; no guarantee of crash-time
erasure is claimed. Scratch content is user content, never telemetry or upload.

Event senders are utterance identities. MainWindow rechecks identity after posting
to the dispatcher, then editor reference/generation before applying a cue.
Old exit/output events cannot stop or clean a replacement utterance. Stop is also
the toolbar action while active; editor hiding, tab switching when available,
focus changes and application shutdown cancel presentation.

The old `OverlaySpeechService` was replaced: it spawned an unowned SAPI process
per click, ignored selected voice/tempo and had no real Stop/Dispose. The candidate
audio button remains and now uses the shared managed route. Source-specific
adapter code remains inside this Windows repository.

Native API references: [OneCore speech options](https://learn.microsoft.com/en-us/uwp/api/windows.media.speechsynthesis.speechsynthesizeroptions),
[System.Speech progress offsets](https://learn.microsoft.com/en-us/dotnet/api/system.speech.synthesis.speakprogresseventargs),
[SSML prosody percentages](https://www.w3.org/TR/speech-synthesis/#S3.2.4).
Executable native timing tests, not these references alone, qualify this adapter.

## Schema 3 migration and rollback

Keep `%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json`; filename is historical.
Read schema 1/2 without writing. Upgrade once in memory, preserving interface/
writing languages, false/unknown consent, voice overrides and unknown properties.
Convert old effective OneCore `1.35^clamp(delta,-1,1)` to its nearest 5% requested
multiplier. Current-schema values use linear 1+delta and are never reconverted.
V1 also receives the existing interface/caret migration. Nonfinite/future/invalid
data remains protected and cannot be overwritten by recovery defaults.

Migration writes first preserve an exact once-only `.v1.bak` or `.v2.bak`
(including BOM), then attempt the existing atomic replace/rolling `.bak`.
A failed replace may leave the original backup without changing the profile.
This does not write on every read or invent consent. Old SAPI integer-rate units
were not a common linear percentage: its tempo can differ after this correction.
The preserved original plus documented formula makes that intentional migration
reviewable; it is not a claim of bit-exact acoustic equivalence across voices.

Recovery: stop the app, keep the current file separately, inspect the appropriate
original `.v1.bak`/`.v2.bak`, and restore only with a schema-compatible application.
Older schema-2 binaries must preserve/refuse schema 3 rather than edit it. Prefer
known-good code that understands schema 3. Production rollback still requires a
higher numeric MSIX package version; no package/update behavior changes here.

## Verification and next work

Run the existing build and `scripts/test_settings_app.ps1` with `-Mode Speech`,
`-Mode SpeechUi`, or All. Adapter tests use synthetic text, actual offline native
voices and measured PCM durations. Desktop tests use isolated profiles and real
Windows input/Notepad; do not interact while they run. They do not attest human
audible quality, Narrator/high contrast, physical mixed DPI, Windows 10 or signed
installation/update. [Slice 4 qualification](qualification/WSA-UX-029_slice4.md)
records source scope, measurements and the manual matrix.

Next slice 5 lets users change suggestion sources/domain lists, configure shortcut
lanes and hear hover previews with reliable cancel. Analysis/local learning,
accounts/Hello and OS updater authority remain later work.

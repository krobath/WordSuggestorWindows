# Automatic unknown-word lookup - WSA-UX-029 slice 12

Date: 2026-11-13. Retain WPF, existing Core CLI and pack format. A06 now has
verified typing paths in the internal editor and the existing supported native
Notepad route. Other external programs remain unsupported. No sharing is enabled.

## Permission and migration

Schema 8 adds IsAutomaticUnknownWordLookupEnabled and
UnknownWordAutomaticConsentVersion (required 1). New and older profiles default
automatic lookup off. Existing schema-7 manual permission stays separate and
cannot authorize capture. Advanced offers an additional default-No explanation:
individual completed Danish words go to ordregister.dk/ordnet.dk, not surrounding
document text. Revoking manual lookup also clears automatic permission. No remote
preference can manufacture local consent. Existing settings/backup/unknown fields
are preserved. Learned words remain available offline after opt-out.

## Verified input and Core

Internal editor accepts a short-lived direct input receipt followed by an exact
matching document change. WPF Space/Enter use their physical key path; preview
text handles punctuation. Paste, opening/restoring a document, suggestion insertion
and programmatic edits cannot manufacture a typing receipt. External observations
come only from WindowsNativeTextAdapter's verified Notepad snapshots: same HWND,
control, PID/start time, no selection and a one/two UTF-16 insertion at the previous
caret with identical surrounding text. Unsupported/password/elevated targets fail
closed in that adapter. No new keyboard hook or clipboard path is added.

The UI only prefilters input events. Core completedWord owns lexical boundaries:
a bounded original 512-unit suffix plus clipping offset yields a UTF-16 range,
never an echoed word. Clipped fragments, URLs/email fragments, known words and
unfinished words produce no candidate. The existing lookup/merge/import path then
validates and persists one word. C# does not tokenize, normalize or rank words.
The context stays local to Core; dictionary HTTP receives only the chosen word.

AutomaticWordLearning has one active operation and no background queue/retry.
At most one dictionary attempt per 3 seconds and 10 per minute; repeated exact
word/language hashes are suppressed for 20 minutes in bounded memory. Known words
do not consume the dictionary budget. Core/HTTP timeouts and cancellation remain
bounded. Different casing can consume another attempt; normalization stays in Core.

A committed prefix may remain valid while the user appends later text. Changing
that prefix, moving caret/selection before it, changing tab/language/settings,
losing verified focus or closing cancels work and prevents late atomic persistence.
Internal final guards include actual foreground HWND, editor/tab identity and
unchanged committed prefix. External guards reread the exact native target.
Results never insert text or change caret/selection. Only saved/unavailable status
is exposed; no document text or unknown-word values are diagnostic fields.

## Evidence and next step

See qualification/WSA-UX-029_slice12.md and the approved closing plan
sprints/WSA-UX-029_closeout_slices12_15.md. Core changes are CLI-only; macOS library,
app source and pack are unchanged. Native macOS execution is not claimed.
Next build Windows sharing against Core's handoff schema. The user assigned backend
implementation to the macOS agent; A07 stays unavailable until that deployment is
qualified. Hello remains user-deferred and installation/human acceptance stay open.

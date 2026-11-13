# Prompt for the macOS / backend agent - WSA-UX-029 slice 13

Implement the missing backend receiver for the EXISTING Core unknown-word handoff
format. The user explicitly assigned backend implementation to you; the Windows
agent has not changed WordSuggestorBackend. Work independently, preserve unrelated
changes and follow repo AGENTS/local docs/change-note and commit instructions.

Core CLI reference: 0278cbc515580f6f9c9e18020e5ed753d9fc7b11 (local Windows
commit, not pushed by this task). The shared handoff library format already exists
in the macOS/Core code; backend work need not wait for the new CLI helper. If the
helper is needed, arrange an authorized push/patch transfer rather than assuming
the commit is already on origin.

## Authoritative inputs

- Core: UnknownWordLearning.swift exportCrowdHandoffPayload and
  UnknownWordCrowdHandoffClient.swift IngestRequest/Validator/RequestBuilder.
- Core CLI: prepareUnknownWordHandoff, consent 1, learnedWordsPath and clientVersion.
  Capability unknownWordHandoffPreparation; it never sends HTTP or writes a file.
- Windows: docs/UnknownWordSharing.md and UnknownWordSharingClient.cs,
  UnknownWordHandoffPacket.cs, AccountUnknownWordSharingSender.cs.
- Existing authenticated API origin https://api.wordsuggestor.com. The release
  catalog/O-052/nginx/beta/Sparkle flow is unrelated and must remain unchanged.

## Required additive contract

Use unknown_word_crowd_handoff_ingest_v1 unchanged. Fields:
schema_version, generated_at_epoch, language, app_bundle, client_version,
client_schema_version (wsx-sq-012-client-v1), retry_policy_version
(wsx-sq-012-retry-v1), idempotency_key, handoff.
Nested handoff follows unknown_word_crowd_handoff_v1 exactly: schema_version,
generated_at_epoch, language_code, learning_state_schema_version, entries, resolved,
diagnostics. Read optional entry fields and snake_case directly from Core Codable
types; do not invent a Windows-only fork. Windows app_bundle is
WordSuggestor.Windows.Beta, while macOS retains its own existing identity.

Choose and document one authenticated relative /v1/... endpoint on the existing
API origin. Return the exact path to Windows; none is guessed in the client.
Expose unknown_word_crowd_handoff_v1_ingest through existing /v1/client/config flags
only after deploy/qualification. Missing/false flag or kill switch prevents use.
Accept only authenticated, permitted current profiles, derive account identity
from the bearer session (never from a submitted account ID). Enforce TLS, rate
limits, max 1MiB request and bounded counts/strings. Reject unknown/duplicate fields,
unsupported schema/language, nonfinite/future timestamps and raw text/pending/failed
content. Allow a documented offline queue window consistent with seven-day retry
retention. Reuse existing auth/error/DB migration/operations conventions.

Require Idempotency-Key matching body idempotency_key (uwh_ + 64 lowercase hex).
Store account-scoped idempotency plus a canonical semantic payload fingerprint
transactionally. Exact replay returns the same successful acceptance without
additional records/counters; conflicting content for the same key returns 409.
Treat Core's key as opaque; do not introduce a differently serialized hash fork.
Upsert/deduplicate existing learned dictionary entries across distinct aggregate
packets as well: newer packets can contain previously delivered words. Do not
count retransmissions as independent users/observations. Bind source/version and
account so one user's data cannot be reassigned to another profile.

Return 202 (or documented existing successful 2xx) after durable atomic acceptance.
408/429/5xx are retryable; other failures stop Windows automatic retries. Do not
log bearer tokens, body, document text, raw words or response details by default.
Retention/deletion/moderation must be explicit. This is quarantine/review input,
not authority to modify ranking, language packs or other users' suggestions.
Do not route the payload into unrelated ambiguity-learning or bug-report schemas;
the lexical-gap future-endpoint note is not an implemented compatible receiver.

## Tests / delivery

Test real Core-produced packet, Unicode, schema rejection, missing/expired/wrong
account auth, idempotent replay/conflict, cross-account separation, item dedupe,
transaction rollback, 413/429/500 and privacy/retention. Keep existing macOS,
auth/settings, beta downloads and Sparkle regression green. Windows clock/retry
preserves exactly one packet and key across updates; ensure this remains compatible.
Document migration, deployment, rollback/kill switch and provide commands using
non-production fixture/account data. Follow the existing macOS backend deploy
procedure; do not deploy or submit production user data without authorization.

Return: backend commit ID, endpoint path, accepted schema/capability, HTTP/error
semantics, deployed version and test evidence, retention/deletion policy and
remaining gates. Windows will keep UnknownWordSharingDeployment.Qualified=false
and EndpointPath=null until actual contract/deployment review and live test-account
qualification. Do not ask Windows to turn it on solely because synthetic tests pass.

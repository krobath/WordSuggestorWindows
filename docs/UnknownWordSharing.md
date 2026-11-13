# Unknown-word sharing client - WSA-UX-029 slice 13

Date: 2026-11-13. Windows client implemented and locally qualified; actual A07
sharing remains unavailable because the user assigned backend work to the macOS
agent. No backend files, production upload or macOS app/library changes.
See handoffs/WSA-UX-029_slice13_backend_agent_prompt.md.

## Activation and consent

UnknownWordSharingDeployment.Qualified=false, EndpointPath=null. No environment,
user JSON or server preference can bypass this source-reviewed gate. A07 remains
disabled/off with an honest explanation. Future activation requires reviewed
backend endpoint, contract tests and /v1/client/config capability
unknown_word_crowd_handoff_v1_ingest, plus current signed-in account and master
lookup permission. No guessed endpoint is contacted. Existing account origin is
https://api.wordsuggestor.com; public beta distribution stays on wordsuggestor.com.

Schema 9 adds IsUnknownWordCrowdHandoffEnabled, UnknownWordSharingConsentVersion:1
and UnknownWordSharingConsentUserId. Older profiles never receive sharing consent.
Future native grant is separate, default-No, explicitly covers existing imported
words and dictionary metadata under the current account. Manual/automatic lookup
permission does not authorize sharing. Logout/lock/account actions cancel pending
account requests; account/profile changes cannot reassign a persisted packet.
Revocation clears only the pending sharing file and backup, never local words.
An HTTP request already accepted by the server cannot be recalled by the app.

## Core and payload

prepareUnknownWordHandoff reuses UnknownWordLearningService's existing
crowdHandoffIngestRequest and RequestBuilder. The wire format remains
unknown_word_crowd_handoff_ingest_v1 / unknown_word_crowd_handoff_v1 with Core-owned
idempotency key, app_bundle WordSuggestor.Windows.Beta and the existing client/retry
policy versions. Export learned dictionary entries only; no document text,
pending/failed raw surface, credentials or manual user words. The memory-only CLI
has no resolved history and reports actual zero pending/failed/resolved counters.
JSON is IPC/storage data, never diagnostics. Packet <=1MiB; closed root/nested
schemas and bounded entry/count shapes are validated without C# lexical rules.

## Queue, transport and failures

UnknownWords/sharing-v1.json under existing LocalAppData root is a schema-1 queue,
max 3MiB including escaped body, with .lock and atomic .bak. Only one aggregate
packet is pending. Bind it to account UserId, API origin and learned-store SHA256.
Keep exact body/idempotency across restart and version update.
A changed/cleared learned source supersedes the old pending packet before sending;
clearing imported words cannot later upload their stale queued copy. A successful 2xx
acknowledgement removes lexical body and backup, retaining only delivery metadata;
unchanged learned data is not resent. New learned data can include earlier entries
again, so the receiver must deduplicate/upsert items without inflating observations.

The existing account backend transport supplies HTTPS origin, bearer credentials
held in memory/secure vault, no redirects/cookies, 12-second deadline and identical
Idempotency-Key/body key. No new credential file or parallel auth protocol.
Missing login, source qualification or independent consent performs no work.
All account operations cancel sharing requests; UI settings/revocation also cancel.
A 30-second timer exists only in a qualified build, coalescing changes and respecting
per-packet exponential backoff (10s base, 1800s cap, five attempts). Offline,
408/429/5xx retry; other errors stop retries. Seven-day pending expiry strips the
lexical queue body when processed; no promise to delete files while the app is off.
Locked/signed-out/unqualified clients do not transmit. Future/corrupt queue or backup
fails closed and is preserved. No secret/raw response/error bodies reach logs.

## Evidence and next work

See qualification/WSA-UX-029_slice13.md. Production path, endpoint, live account
upload and server persistence are NOT qualified. Do not flip the source gate based
on mock success. The macOS backend agent must return commit, exact path/capability,
auth and idempotency evidence, deployed HTTPS checks and rollback/kill-switch plan.
Then Windows can qualify the real receiver under separate test-account consent.
Next closing slice qualifies installer/update behavior; Hello remains user-deferred.

# Windows account, session and Hello contract

WSA-UX-029 slice 7, 2026-11-11. Implementation candidate; positive native Hello
qualification remains open on the current host. WPF/.NET 10 and the existing
Core/pack boundary are retained. Backend reference is `2f67ddc6`; canonical macOS
Settings/presentation reference is `ba18b45`. No backend/Core protocol is added.
See [qualification](qualification/WSA-UX-029_slice7.md).

## Profile controls and authority

P01 shows backend-confirmed profile identity only with a valid, unexpired access
session. Remembered email and a protected recovery record are not login proof.
P02-P05 switch login/verification-first registration and use PasswordBox controls;
passwords are cleared when submitted and when the account view closes. Registration
requires `email_verification_v1_issuance`. Disabled/unknown capabilities never send
a registration request. Legacy access-only login is supported in memory only.

P06 uses backend pending status and a bounded resend cooldown. The UI updates the
cooldown without another request. Browser email verification never attests an app
session; sign in again or explicitly reload status. No token-bearing callback URL
is accepted by the Windows app.

P07 protects local session recovery with Windows Hello PIN/biometrics; off means
automatic recovery, not logout. The internal policy also recognizes the legacy
`loginAtLaunch` value and does not silently restore it. Effective Hello protection
is committed only after native key creation, reopening, deterministic protected
operations and an encrypted-data roundtrip succeed. API availability alone never
sets the persisted policy. Missing provisioning or cancellation leaves the prior
credential intact. Local lock, view close, OS session lock and app disposal clear
unlocked key/access state; reopening requires explicit Hello again.

P08-P13 are drafts with explicit Save. Catalog IDs match the canonical macOS and
backend catalogs, with localized Windows labels. Unknown existing IDs are retained
and may be resubmitted unchanged; clients cannot invent new IDs. Personalization
consent stores metadata and changes no ranking, language pack or suggestion lane.
The existing name and personalization endpoints are two transactions. If the
second fails or conflicts, the app explicitly reports partial success and retains
the edited draft. Reload asks before discarding a dirty draft. Optimistic version
checks are retained; failures are not reported as a successful combined save.

P14 shows authoritative email/verification. P15 uses `email_change_v1`, matching
new-email fields and current password. Pending state never changes displayed
email. Cancel remains available after restart because the backend has no pending
request-query endpoint; its cancellation operation is authoritative/idempotent.
P16-P17 require current password even with Hello, matching new values and the
existing minimum 12/uppercase/lowercase/digit policy. Backend remains authoritative.
Success clears local credentials and requires fresh login.

P18 displays only existing entitlement product/tier/status/expiry, or unavailable.
No billing/license model or local entitlement grant is added. P19 signs out this
PC, including while locked/offline. P20 requires explicit native confirmation and
server acknowledgement before reporting all-device logout. Document buffers,
settings, user words, packs, learning records and insights are independent.

P21 distinguishes signed out, connecting, authenticated, locked, offline recovery
and reauthentication. One operation runs at a time. Closing/locking cancels and
invalidates pending results; a late response cannot replace credentials. Local
Core suggestions continue without a backend session.

## Existing backend wire contract

The default origin is `https://api.wordsuggestor.com`. The transport rejects
non-HTTPS origins, URL credentials and invalid paths; TLS certificate validation
is not overridden, redirects/cookies are disabled, requests time out and responses
are capped at 64 KiB. Error bodies and transport exception messages are never
forwarded into UI/logs. Backend JacksonConfig applies **snake_case to all APIs**.

| Operation | Existing endpoint |
|---|---|
| Capabilities | GET `/v1/client/config` (`flags`, `kill_switch`) |
| Login / rotating recovery | POST `/v1/auth/token`, `/v1/auth/refresh` |
| Verification-first registration / resend | POST `/v1/auth/registrations`, `/v1/auth/email-verification/requests` |
| Profile / optimistic personalization | GET/PUT `/v1/client/profile`, `/v1/client/profile/personalization` |
| Entitlements | GET `/v1/client/entitlements` |
| Password | PUT `/v1/auth/password` |
| Email change / cancel | POST `/v1/auth/email-change/requests`, DELETE `/v1/auth/email-change` |
| Current / all sessions | DELETE `/v1/auth/sessions/current`, `/v1/auth/sessions` |
| Offline revocation | POST `/v1/auth/session-revocations` |

Login negotiates `refresh_session_v1` with a stable device ID and client version.
Recovery validates origin, device/session identifiers, user ID, contract, token
type and expiry. Refresh persists its idempotency key **before** sending; a timeout
retries the same old token/key, and rotation clears that key only after storing
the new recovery credential. A response cannot switch user/session identity.
Terminal 401/refresh 403 and inconsistent identity responses fail closed. Password
policy errors, conflicts and offline states cannot grant a session.

## Storage and cryptographic boundary

Only the nonsecret device GUID is a file:
`%LOCALAPPDATA%\WordSuggestor\account\device-id-v1.txt`. Existing app data remains
unchanged. No credential fields are added to Settings schema 5. Corrupt/inaccessible
identity data reports a bounded account error while other Settings remain usable.

Refresh/revocation credentials are in per-user Windows Credential Manager generic
credentials, persisted locally. The target namespace is a SHA-256 of origin and
the app data root; test roots use separate namespaces. The bounded v1 envelope
contains policy and recovery data; access tokens/passwords are never persisted.
Unsupported/corrupt envelopes block normal reads/writes and are preserved.
Credential Manager's 2,560-byte limit fails closed; no file/plaintext fallback.
Managed password/token strings are temporary process memory; byte/native buffers
are cleared where feasible. This does not claim protection against a compromised
unlocked process or Windows account.

In Hello mode the recovery payload is AES-256-GCM ciphertext with a random nonce
and AAD binding origin, policy, key name/challenge and revocation metadata. The
key derives through HKDF-SHA256 from a per-vault Windows Hello RSA/PKCS1 signature
of a fixed random challenge. Here that signature is secret KDF input: it is never
persisted, logged, uploaded or reused as a public authentication signature.
The native signature is checked against its RSA public key and the key is never
exported from Windows. Derivation must repeat after reopening before enabling;
incompatible/nondeterministic providers fail without changing prior data.

Microsoft documents Hello's protected signing and RSA/SHA256/PKCS1 verification
in its [native Hello service tutorial](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello-auth-service).
The local encryption/KDF composition is this Windows adapter's design, not a new
backend authentication protocol or a Microsoft attestation of this application.
Positive native PIN/biometric execution remains a release qualification gate.

The embedded Windows-only WinRT worker runs through an absolute system PowerShell
path with no shell expansion, hidden window, bounded pipes and timeout/process-tree
cleanup. Input contains key metadata/challenge; output signature material stays
in anonymous pipes/process memory. No temporary script/secret files or credential
command-line arguments are created. Failed/disabled/deleted owned keys receive
best-effort bounded cleanup; an unavailable provider may leave an unusable orphan
key, containing no app token. Cleanup failure never restores a deleted credential.

Revocation-only metadata remains outside the Hello ciphertext, still inside
Credential Manager. It can revoke but cannot restore the session. Thus locked
offline logout can remove recovery immediately without a PIN prompt and queue
revocation separately. A maximum four revocation-only items survive in a secure
slot until reconnect/login acknowledges them. Queue/storage failure is reported;
the app never claims all-device revocation while offline. No user/document data
is part of that queue.

An access-only legacy session supplies no offline-revocation secret. Its failed
server logout therefore cannot be queued: local access is still removed, but
server revocation remains unconfirmed and must be checked after reconnect/login
or through the account's all-device logout. No legacy global logout is silently
substituted for the current-device action.

## Build, test and troubleshooting

Use the repository build/run scripts and private pinned SDK, without production
secrets. In an unlocked ordinary-user desktop:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode AccountState
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode AccountUi -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode All -SkipBuild
# Explicit, interactive; uses only a unique synthetic credential/key namespace:
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode HelloNative -SkipBuild
```

The account state/UI tests inject an HTTP handler with the exact existing JSON
contract and synthetic identities. Real Credential Manager is tested separately.
Mock key operations prove failure/transaction handling, not PIN/biometric access.
`HelloNative` is deliberately excluded from unattended `All`; it prompts for
native key operations and tests passive-lock rejection, restart recovery, protected
rotation and disabling protection. Exit 3 means blocked, not PASS. A successful
test deletes its synthetic credential/key. No real account or backend is mutated.

For the current blocked host, use a **local unlocked Windows session with an
enrolled Hello PIN/biometrics**, then rerun `HelloNative`. Do not enroll credentials
through an agent or send PINs/passwords in chat. Unpackaged/MSIX behavior, actual
cancel/lockout/biometric hardware and a dedicated backend test account remain
explicit integration gates. RDP availability is not console qualification.

For profile conflicts, preserve the draft and reload before retrying. For offline
logout, reconnect/reload to retry secure revocations. For storage errors, inspect
permissions/version with metadata only; never export Credential Manager contents,
tokens, passwords or recovery payloads into logs/support reports. No raw backend
body or secret is needed to diagnose the status code.

Next engineering slice: implement General's update controls against App Installer's
actual authority. Native Hello and live dedicated-account acceptance stay open;
this delivery authorizes no production publishing, signing or payment changes.

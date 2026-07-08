# Persistent Session Restore Design

Issue: #21

## What already exists

Most of #21 was already implemented and is left unchanged:

- **Secure, user-scoped refresh-token storage** — `UserScopedTokenStore` writes
  `tokens.dat` under `%LocalAppData%\SonicRelay\WindowsPublisher`, DPAPI-protected
  (`CryptProtectData`, current user). No plaintext tokens anywhere.
- **Refresh + one-shot 401 retry** — `ApiHttpClient` sends with the access token,
  and on `401` (with a refresh token present) refreshes once via `/auth/refresh`,
  persists the new tokens, and retries the original request once.
- **`/auth/me`** via `AuthApiClient.GetCurrentUserAsync`.
- **Device reuse** — `PublisherWorkflow` matches this machine's existing
  `windows_publisher` device by hostname and only registers when none exists, so
  restarts don't create duplicate devices.
- **Logout** clears stored tokens; **corrupt/unavailable storage** is handled via
  `TokenStorageResult` (`Failed` / `SecureStorageUnavailable`).

## The gap

On launch the app builds the runtime but **never restores the authenticated
session**, so the user must sign in again every time even though a valid refresh
token is on disk. This violates the core acceptance criteria ("reopening/rebooting
keeps the user authenticated"). This change adds the missing startup restore.

## Change

- Extract the post-login tail of `SignInAndPrepareDeviceAsync` into
  `PrepareAuthenticatedStateAsync(CancellationToken)` — calls `/auth/me`, resolves
  (reusing) this machine's publisher device, and sets the authenticated snapshot.
  Login now = `auth.LoginAsync(...)` then `PrepareAuthenticatedStateAsync`.
- Add `PublisherWorkflow.RestoreSessionAsync(CancellationToken)`:
  - Calls `PrepareAuthenticatedStateAsync`. `GetCurrentUserAsync` is authenticated,
    so `ApiHttpClient` transparently refreshes an expired access token once using
    the stored refresh token.
  - On `ApiClientException{ Unauthorized }` (no stored session, or the refresh
    token is no longer valid) it clears local auth state (`auth.LogoutAsync`) and
    resets to a fresh unauthenticated snapshot → the login screen.
  - On network/backend errors it silently stays unauthenticated (no alarming
    error banner) so the user can retry.
- `App.ConfigureBackendAsync` fires `RestoreSessionAsync()` right after a runtime
  is created (covering both startup and a backend-URL change), non-blocking; the
  UI reacts to `StateChanged`.

Auth metadata (user id/email/display name, device id) is re-derived from `/auth/me`
+ `/devices` on restore rather than persisted separately — always fresh and no
extra at-rest data.

## Tests (`dotnet test`)

- `RestoreSessionAsync` with a valid stored session ⇒ authenticated + device
  prepared (reuses an existing matching device without registering).
- `RestoreSessionAsync` when `/auth/me` returns Unauthorized ⇒ stays
  unauthenticated, clears tokens (`LogoutAsync` called), no error banner.
- `RestoreSessionAsync` on a network error ⇒ stays unauthenticated without an
  error banner.

## Acceptance criteria

- Reopening the app / rebooting keeps the user authenticated when the refresh
  token is still valid; an expired access token is refreshed automatically; an
  invalid refresh token returns the user to login; device registration is reused.
  Tokens never logged or stored in plaintext. `dotnet build` + `dotnet test` pass.

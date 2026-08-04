---
paths:
  - "src/Common/EvilCase.Auth/**"
---

# Authentication

Access token in memory, refresh token in a cookie; all of it behind `IAuthService`. The
controller only maps results to status codes and moves the cookie in and out.

- `AuthSessionId`, never `SessionId` — entity, column, contract, log templates. `XSessionId`
  already names the browser session in the logging pipeline.
- Rotation spends a token with the atomic `UPDATE … WHERE RevokedAt IS NULL`, never the read
  before it. A token spent again is a replay and ends the chain; inside the 30-second race grace
  it is only refused, and the response leaves the cookie alone.
- The CSRF defence is `SameSite=Strict` plus same-origin; there is no antiforgery token.
- Registration is closed. `Auth:Seed` creates the first administrator only into an empty user
  table and never overwrites.
- Default deny: the authorization fallback policy makes every unattributed endpoint require
  authentication. The `[AllowAnonymous]` list is pinned by `AuthorizationFallbackTests`;
  extending it is an owner decision.
- In the browser, only `login`, `refresh` and `logout` skip token renewal; the `[Authorize]`
  endpoints under `/api/auth` renew like any other. Everything under `/api/auth` is sent with
  credentials, the 401 retry included. `AuthTokenHandler` resolves `IAuthSession` on use, not in
  its constructor.

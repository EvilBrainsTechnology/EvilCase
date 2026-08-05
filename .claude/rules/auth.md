---
paths:
  - "src/Common/EvilCase.Auth/**"
  - "src/App/EvilCase.App/Auth/**"
  - "src/Api/**"
---

# Authentication

Access token in memory, refresh token in a cookie; all of it behind `IAuthService`.

- `AuthSessionId`, never `SessionId` — entity, column, contract, log templates. `XSessionId`
  already names the browser session in the logging pipeline.
- Rotation spends a token with the atomic `UPDATE … WHERE RevokedAt IS NULL`, never the read
  before it; `RefreshTokenTests` pins what that buys.
- The CSRF defence is `SameSite=Strict` plus same-origin; there is no antiforgery token.
- Registration is closed. `Auth:Seed` creates the first administrator only into an empty user
  table and never overwrites.
- Default deny: the authorization fallback policy makes every unattributed endpoint require
  authentication. The `[AllowAnonymous]` list is pinned by `AuthorizationFallbackTests`;
  extending it is an owner decision.

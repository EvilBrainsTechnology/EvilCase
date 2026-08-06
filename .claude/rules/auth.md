---
paths:
  - "src/Common/EvilCase.Auth/**"
  - "src/App/EvilCase.App/Auth/**"
  - "src/Api/**"
---

# Authentication

Access token in memory, refresh token in a cookie; all of it behind `IAuthService`.

- `AuthSessionId`, never `SessionId` — entity, column, contract, log templates.
- Rotation spends a token with the atomic `UPDATE … WHERE RevokedAt IS NULL`, never the read
  before it.
- The CSRF defence is `SameSite=Strict` plus same-origin; there is no antiforgery token.
- Registration is closed. `Auth:Seed` creates the first administrator only into an empty user
  table and never overwrites.
- Every unattributed endpoint requires authentication. Extending the `[AllowAnonymous]` list is
  an owner decision.

---
paths:
  - "src/Common/EvilCase.Auth/**"
  - "src/App/EvilCase.App/Auth/**"
  - "src/Api/**"
---

# Authentication

Access token in memory, refresh token in a cookie; all of it behind `IAuthService`.

- `AuthSessionId`, never `SessionId` — entity, column, contract, log templates.
- A write a decision rests on is one statement against the stored row, never a value from the
  read before it: rotation spends a token with `UPDATE … WHERE RevokedAt IS NULL`; the
  failed-login counter increments in the database and the lockout follows what it returns.
- The CSRF defence is `SameSite=Strict` plus same-origin; there is no antiforgery token.
- Registration is closed. `Auth:Seed` creates the first administrator only into an empty user
  table and never overwrites.
- Every unattributed endpoint requires authentication. Extending the `[AllowAnonymous]` list is
  an owner decision.

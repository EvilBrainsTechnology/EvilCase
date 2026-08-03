# Authentication

Access token in memory, refresh token in a cookie. `EvilCase.Auth` holds all of it behind `IAuthService`; the controller only turns results into status codes and moves the cookie in and out.

- **Access token** — HS256 JWT, 15 minutes, returned in the response body and kept in the browser's memory only. Claims: `sub` (id), `unique_name` (e-mail, the name claim), `role`, `sid` (the session), `jti`. `MapInboundClaims` is off, so those are also the types on the principal; `AuthClaims` in `EvilCase.Api.Contract` names them for both halves.
- **`AuthSessionId`, never `SessionId`** — the identifier of a rotation chain, in the entity, the column, the contract and every log template. The logging pipeline already carries a browser session as `XSessionId` (`RequestContextPropertyNames.SessionId`), and one log event holds both.
- **Refresh token** — 32 bytes of randomness in `__Host-evilcase-refresh`: `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/`. Only its SHA-256 is stored. `SameSite=Strict` plus same-origin-only is the whole CSRF defence; there is no antiforgery token.
- **Rotation** — every refresh spends the token and issues another inside the same `SessionId`. Spending it is the atomic `UPDATE ... WHERE RevokedAt IS NULL`, not the read before it, so of two callers holding the same token exactly one is served. A token presented after it was revoked is a replay and ends that whole chain. Inside a 30-second grace window it is instead read as two tabs racing (`RefreshStatus.Raced`) and only refused — and the response leaves the cookie alone, because it already holds the winner's replacement and a delete matches by name.
- **Lifetimes** — a refresh token is good for 14 days, a rotation chain for 30 from sign-in whatever it does in between. Both under `EvilBrains:EvilCase:Auth:RefreshToken`.
- **Lockout** — 5 consecutive failures lock an account for 15 minutes (`Auth:Lockout`). The counter starts over with the lockout. Sign-in answers `401` for bad credentials and `423` for a lockout; the client branches on the status code and never on a message.
- **Seeding** — registration is closed and there is no register endpoint. `Auth:Seed` (e-mail and password) creates the first administrator at startup, only while the table holds no user at all. It never overwrites. That administrator is the only way into a fresh database; `.claude/skills/run-app/SKILL.md` has the sign-in sequence.

## Default deny

`AddEvilCaseAuth` sets an authorization fallback policy, so an endpoint with no attribute needs an authenticated caller. What stays open says so with `[AllowAnonymous]`: both `/health/*`, the API 404 fallback, `MapFallbackToFile("index.html")`, `/scalar` and `/openapi/v1.json`, `LogsController` (the frontend logs from the sign-in page too) and the sign-in, refresh and sign-out endpoints. `Tests/EvilCase.Tests/Hosting/AuthorizationFallbackTests` pins the list.

Adding `[AllowAnonymous]` anywhere, or placing a page outside `MainLayout`, is a decision for the owner, never a silent choice.

## In the browser

`EvilCase.App/Auth`: `AccessTokenStore` holds the token in memory, `EvilCaseAuthenticationStateProvider` is both the state provider and `IAuthSession`, and `AuthTokenHandler` attaches the bearer, renews a minute before expiry and retries once after a `401`. Its first `GetAuthenticationStateAsync` calls refresh, which is what signs the user back in after a reload. The handler resolves `IAuthSession` on use rather than in its constructor — the renewal goes through a generated client that has the handler in its own chain.

Only the three anonymous endpoints (`login`, `refresh`, `logout`) skip the renewal, because renewal itself goes through them; the `[Authorize]` ones under `/api/auth` are renewed like any other, or `logout-all` would fail silently on an expired token and leave every other device signed in. Paths are matched by segment, and everything under `/api/auth` is sent with `BrowserRequestCredentials.Include` — including the retry, which copies `HttpRequestMessage.Options` along with the headers and the buffered body.

The application is closed by default on the client too, and `MainLayout` is what does it: everything it lays out sits inside an `AuthorizeView`, so a new page is protected without doing anything. Escaping that means choosing another layout, which only `Pages/Login.razor` does (`LoginLayout`).

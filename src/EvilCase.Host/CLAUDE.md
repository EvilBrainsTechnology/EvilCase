# The host

Composition root: `Program.cs`, the middleware pipeline and all configuration.

The rules for everything in here — pipeline order, the two `Hosting` keys, security headers, rate
limits, `.env` loading and `MigrateOnStartup` — are in `src/Api/CLAUDE.md`, which covers the API and
the host together because the host exists to serve the API.

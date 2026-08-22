---
paths:
  - "src/**"
---

# Code

- Prefer clean, readable code to exhaustive correctness: no null check on what cannot be null, no
  `try`/`catch` that only rethrows, no argument validation inside an internal method.
- `ArgumentNullException.ThrowIfNull` never guards a non-nullable parameter.
- No `Async` suffix. Exceptions: a genuine sync/async pair on one surface, and names not ours to
  choose (`SendAsync`, `DisposeAsync`, `OnAfterRenderAsync`).
- Every class resolved from DI is `internal sealed` and consumed through an interface; a public
  consumer gets a public interface with an internal implementation. Exceptions: types the
  framework instantiates by concrete type or with no service role — controllers,
  `DelegatingHandler` subclasses, middleware, exceptions, DTO and options records, static
  helpers.
- One type per file.
- Fix an analyzer finding; never suppress one to get a build green.
- A test's assertion message names the broken rule, in a clause.
- A namespace and an assembly carry the `EvilBrains.*` prefix, so a type sits under a longer
  namespace than its folder suggests.
- The owner decides first on a destructive migration, a dependency change, a change to
  authentication or the security headers, and a rewrite of something that works.

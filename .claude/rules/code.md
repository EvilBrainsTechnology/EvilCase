---
paths:
  - "src/**"
---

# Code

- Prefer clean, readable code to exhaustive correctness: no null check on what cannot be null, no
  `try`/`catch` that only rethrows, no argument validation inside an internal method.
- No type or method for a single call site; a helper folds into its only consumer.
- No machinery before a caller needs it; a one-line comment marks what is deferred.
- `ArgumentNullException.ThrowIfNull` never guards a non-nullable parameter.
- No `Async` suffix. Exceptions: a genuine sync/async pair on one surface, and names not ours to
  choose (`SendAsync`, `DisposeAsync`, `OnAfterRenderAsync`).
- A name says the thing itself; no `Application` prefix outside the `ApplicationDbContext` types.
- `Parse` throws on invalid input; `ParseOrDefault` returns the default.
- Every class resolved from DI is `internal sealed` and consumed through an interface; a public
  consumer gets a public interface with an internal implementation. Exceptions: types the framework
  instantiates by concrete type or with no service role — controllers, `DelegatingHandler`
  subclasses, middleware, exceptions, DTO and options records, static helpers.
- One type per file.
- A convention is configured over the whole model, never property by property.
- Required beats nullable; a property is nullable only where the domain allows its absence.
- A member carries a block body; an arrow body only for a property or indexer on one line.
- A call chain over more than one line starts every call on its own line, the first one included.
- Never a static method that takes what the instance already holds.
- Resolve a service into a local, then call it; never chain off `GetRequiredService<T>()`.
- Fix an analyzer finding; never suppress one to get a build green.
- A test's assertion message names the broken rule, in a clause.
- A namespace and an assembly carry the `EvilBrains.*` prefix, longer than the folder suggests.
- The owner decides first on a destructive migration, a dependency change, a change to
  authentication or the security headers, and a rewrite of something that works.

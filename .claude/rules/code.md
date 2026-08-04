---
paths:
  - "src/**"
---

# Code

- Clean, readable code sometimes beats 100 % correctness and defensiveness.
- No `Async` suffix. Exceptions: a genuine sync/async pair on one surface, and names not ours to
  choose (`SendAsync`, `DisposeAsync`, `OnAfterRenderAsync` and the like).
- Every class resolved from DI is `internal sealed` and consumed through an interface; a public
  consumer gets a public interface with an internal implementation. Exceptions: types the
  framework instantiates by concrete type or with no service role — controllers,
  `DelegatingHandler` subclasses, middleware, exceptions, DTO and options records, static
  helpers.
- One type per file.
- Analyzers run at error severity (Meziantou, Roslynator, EvilBrains `EB0001`–`EB0004`,
  `EB1001`–`EB1016`). Fix findings; do not suppress without reason.
- A test's assertion message names the broken rule, in a clause.
- Call `ILogger` directly with a constant message template; CA1848 is off and `[LoggerMessage]`
  is not used.
- Package versions live only in `src/Directory.Packages.props`.
- Namespaces and assemblies are auto-prefixed `EvilBrains.*` by `src/Directory.Build.props`.

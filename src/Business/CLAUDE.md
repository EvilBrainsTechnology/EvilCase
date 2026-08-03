# Business and the domain kernel

Business logic lives in `EvilCase.Business` and nowhere else. The frontend renders and collects input; it never decides anything the server would have to agree with.

```
EvilCase.App  →  EvilCase.Api.Client  →  (HTTP)  →  EvilCase.Api  →  EvilCase.Business  →  EvilCase.Data
                                                          ↓                 ↓                   ↓
                                                  EvilCase.Api.Contract → EvilCase.Domain ←──────┘
```

- **`EvilCase.Domain` references nothing**, not even the contract. It holds the vocabulary two layers have to agree on — `CaseStatus`, `ActDirection`, `PartyKind`, `UserRole` — and is small enough to ship to the browser through the contract.
- **`EvilCase.Data` is schema.** Entities, the `DbContext`, its configuration and the migrations. No rule, no projection into a wire DTO, and no reference to `EvilCase.Api.Contract` — that reference is what let the list query in an earlier draft select straight into `CaseListItem`.
- **`EvilCase.Business` owns the rules and the queries**, including the composable `IQueryable` steps: what a root case is, what "open" means, how a search term is escaped. It references EF Core, and a query composes and materialises in one place rather than being handed half-built to a caller.
- **A business service returns the contract DTO.** No second set of result models and no mapping layer: the projection stays a single SQL statement and the controller is a line long. The cost is deliberate — a change to the wire shape reaches into `EvilCase.Business`.
- **`EvilCase.Api` is HTTP and nothing else.** Route, binding, status code. It has no reference to `EvilCase.Data` and a controller never sees a `DbContext` or an `IQueryable`.
- **`EvilCase.Auth` stays outside this.** It is a closed module behind `IAuthService` with its own entities, and it is exempt because it is about security rather than because it is different in kind.
- **`Tests/EvilCase.Tests/Architecture/LayerTests` pins the direction of every arrow.** An assembly reference exists only where a type of it is used, so the test fails on the code that breaks the layering rather than on an intention to.

Where a rule is pure — a walk over a loaded case graph, an act's date fallback, the shape of a generated file mark — it is a static class in `EvilCase.Business` with no `DbContext` in sight, and it is tested without one.

## Ownership

- **`IOwnerContext` is the one place ownership is resolved.** `EvilCase.Business` declares it; `PrincipalOwnerContext` in `EvilCase.Api` implements it by reading the access token's `sub` claim, and is the only code in the application that reads that claim for this purpose. A query needing the owner takes `IOwnerContext`, never an `ownerId` parameter threaded down from a controller and never `HttpContext` of its own.
- **`OwnerId` throws, `OwnerIdOrDefault` does not.** Code that has no sensible behaviour without an owner takes the first: a query that would otherwise return another owner's rows, or silently none, is a bug either way. The second is for the callers where absence is normal — a health probe, the sign-in endpoint, a migration at startup.
- **This is where a tenant goes.** When the vision's multi-tenant horizon becomes real, an owner becomes a tenant and this interface is what changes, rather than every query in the application.

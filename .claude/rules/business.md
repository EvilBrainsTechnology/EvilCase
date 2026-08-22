---
paths:
  - "src/Api/**"
  - "src/Business/**"
  - "src/Data/**"
---

# Layers

```
App → Api.Client → (HTTP) → Api → Business → Data
                             ↓        ↓        ↓
                       Api.Contract → Domain ←─┘
```

`Tests/EvilCase.Tests/Architecture/LayerTests` pins every arrow and says what each one is for.
What it cannot see:

- The frontend renders and collects input. It never decides.
- A business service returns the contract DTO — no second model set, no mapping layer.
- `EvilCase.Auth` is a closed module behind `IAuthService`, exempt from the layering.
- A pure rule is a static class with no `DbContext` in sight, tested without one.

## Queries

- One `IQueryable` extension step per rule, composed by a reader; a step returns `IQueryable`
  and ends at the ordering.
- The reader projects straight into the contract DTO and calls `ToListAsync` once, at the end.
- A read that yields one row is one step that returns the row, not an `IQueryable`.

## Tenancy

`IUserContext` is the only place tenant and user resolve; a query takes it, never a `tenantId`
parameter or `HttpContext`. Every tenant entity implements `ITenantEntity`, carries a query filter
on `TenantId` and leads its unique indexes with it. The user is the one that carries neither: a
sign-in reaches it by e-mail alone, unique across the deployment. `SaveChanges` refuses another
tenant's row, fills a user-owned row's `UserId` from it and refuses another user's row. Work
outside a request enters both ids at once.

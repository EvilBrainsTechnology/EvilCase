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

## List queries

- One `IQueryable` extension step per rule, composed by a reader; a step returns `IQueryable`
  and ends at the ordering.
- The reader projects straight into the contract DTO and calls `ToListAsync` once, at the end.

## Tenancy

`ITenantContext` is the only place the tenant is resolved; a query takes it, never a `tenantId`
parameter or `HttpContext`. Every tenant entity implements `ITenantEntity`, carries a query filter
on `TenantId` and leads its unique indexes with it; `SaveChanges` refuses a row of another tenant.
Work outside a request names its tenant with `Enter`.

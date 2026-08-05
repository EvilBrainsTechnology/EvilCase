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

- Business logic lives in `EvilCase.Business` and nowhere else; the frontend renders and
  collects input, it never decides.
- `EvilCase.Domain` references nothing.
- `EvilCase.Data` is schema only — entities, `DbContext`, configuration, migrations — and never
  references `EvilCase.Api.Contract`.
- `EvilCase.Business` owns the rules and the queries; a query composes and materialises in one
  place. A business service returns the contract DTO: no second model set, no mapping layer.
- `EvilCase.Api` is HTTP only — route, binding, status code. A controller never sees a
  `DbContext` or an `IQueryable`.
- Health checks chain per layer, each `Bootstrap` forwarding to the one below; the host calls
  the top and names the tag.
- `EvilCase.Auth` is a closed module behind `IAuthService`, exempt from the layering.
- `Tests/EvilCase.Tests/Architecture/LayerTests` pins every arrow.
- A pure rule is a static class with no `DbContext` in sight, tested without one.
- Only one layer of a case's relations is ever read; nothing walks past it.

## List queries

- One `IQueryable` extension step per rule, composed by a reader; `ToListAsync` once, at the
  end. The projection selects straight into the contract DTO.
- A search term is text, not a pattern: escape `%` and `_` and name the escape character in the
  `ILIKE`. Case folding belongs to `ILIKE`, never to `ToLower()`.
- Test the SQL through `ToQueryString()`, no server needed; see `CaseListQueryTests`.

## Ownership

- `IOwnerContext` is the only place ownership is resolved; a query takes it, never an `ownerId`
  parameter or `HttpContext`. `PrincipalOwnerContext` in `EvilCase.Api` implements it from the
  access token's `sub` claim.
- `OwnerId` throws; `OwnerIdOrDefault` is for callers where absence is normal — a health probe,
  sign-in, a startup migration. A future tenant lands in this seam.

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
- A business service returns the contract DTO — no second model set, no mapping layer. The cost
  is that a change to the wire shape reaches into `EvilCase.Business`.
- Health checks chain per layer, each `Bootstrap` forwarding to the one below; the host calls
  the top and names the tag.
- `EvilCase.Auth` is a closed module behind `IAuthService`, exempt from the layering.
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
  sign-in, a startup migration.
- Nothing in the schema keeps a `CaseRelation` inside one owner: the write resolves both ends
  through `IOwnerContext`, and the read and the delete are owner-scoped too.

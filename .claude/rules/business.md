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

- One `IQueryable` extension step per rule, composed by a reader; `ToListAsync` once, at the
  end. The projection selects straight into the contract DTO.
- A search term is text, not a pattern: escape `%` and `_` and name the escape character in the
  `ILIKE`. Case folding belongs to `ILIKE`, never to `ToLower()`.
- Test the SQL through `ToQueryString()`, no server needed. `CaseReaderTests` pins what a reader
  really runs; `CaseListQueryTests` pins one step.

## Ownership

`IOwnerContext` is the only place ownership is resolved; a query takes it, never an `ownerId`
parameter or `HttpContext`. Where the schema cannot keep a row inside one owner, the write is
what enforces it, and the read and the delete are owner-scoped too.

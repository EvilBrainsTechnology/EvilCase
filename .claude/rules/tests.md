# Tests

- A test guards a layering or convention rule no analyzer enforces, a security boundary
  (authentication, tenant isolation, cross-site writes, file access, signature verification),
  non-trivial logic (an algorithm, parsing, numbering, concurrency), database behaviour (a query,
  a constraint, a trigger, a migration) or a fixed bug. One behaviour is tested on one layer.
- Never a test asserting markup, CSS classes, texts or layout for their own sake, a pass-through
  read or write, a DTO mapping, or what the compiler or an analyzer enforces.
- Every page has one smoke render test per mode (view, edit, new) and a test of each logic it
  owns; a page or component test observes that logic through the rendered output.
- A test that reaches the database runs on PostgreSQL, never on a fake `DbContext`, and asserts the
  rows it returns; it reads the generated SQL only for a rule no result reaches — the columns read,
  an aggregate, paging, an order key no two rows can tie on. Pure logic stays off it (`business.md`).
- An assertion message names the broken rule, in a clause, where the assertion alone does not.

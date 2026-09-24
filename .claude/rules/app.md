---
paths:
  - "src/App/**"
---

# Frontend

`EvilCase.App` is Blazor WebAssembly. A screen composes the `Ec` primitives from `Components/Ec`,
styled by `wwwroot/css/ec-components.css` over the values in `wwwroot/css/ec-tokens.css`. SDD-020
rules the look, `docs/design/` is binding on values.

- A style belongs in `ec-components.css` and a value in `ec-tokens.css`: no inline style, no
  literal colour or size, no class of a foreign library.
- Modal and date are native: `EcModal` over `dialog`, `input type="date"`.
- `Icons/AppIcons.cs` is the only icon source and holds only the icons the app uses.
- A new page goes inside `MainLayout`, which authenticates it; outside it is an owner decision.

## Responsive

- Desktop is primary. A screen used daily is first-class on mobile, the rest must only not break.
- One breakpoint: `lg` (992 px), never mixed with `md`; never branch layout in C# or JS by viewport.
- Data lists never scroll horizontally: below `lg` a row reflows onto two or three lines and
  sorting moves into a select in the toolbar.
- Touch targets ≥ 44 px below `lg`. Form action buttons sticky at the bottom,
  `env(safe-area-inset-bottom)` on fixed bottom elements. A tooltip never carries information alone.
- Unavoidable JS goes through an `IJSObjectReference` disposed in `IAsyncDisposable`.

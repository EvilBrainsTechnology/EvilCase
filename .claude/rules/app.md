---
paths:
  - "src/App/**"
---

# Frontend

`EvilCase.App` is Blazor WebAssembly on TabBlazor, components over the Tabler CSS framework.

- `index.html` must link `EvilBrains.EvilCase.App.styles.css` — several TabBlazor components
  ship their CSS as scoped styles and silently render unstyled without it.
- Tabler CSS and popper.js are vendored under `wwwroot/lib/` at versions matching the TabBlazor
  release; no CDN. Update by downloading the matching version.
- `Icons/AppIcons.cs` holds only the icons the app uses; add one by copying its path data from
  the Tabler icon set. Never vendor the whole set.
- Navigation marks the active item by setting `active` on the `li` — Tabler draws the indicator
  on `.nav-item.active`, not on Blazor's `NavLink`.
- Theme goes through `TablerService.SetTheme`; the initial theme comes from an inline script in
  `index.html`, so changing it changes the CSP — `.claude/rules/api.md`.
- A new page goes inside `MainLayout`, which authenticates it; placing a page outside it is an
  owner decision.
- `host.StartClientLogging()` stays after `builder.Build()` — without it browser events buffer
  and are silently dropped.

## Responsive

Desktop is primary. Mobile is first-class for the case list, case detail, deadlines and quick
notes; administrative flows only must not break.

- One breakpoint: `lg` (992 px), never mixed with `md`. Modals: `modal-fullscreen-lg-down`.
- Data lists never scroll horizontally on mobile: render a table and a card variant and switch
  by CSS only (`d-none d-lg-block` / `d-lg-none`); `Pages/Home.razor` is the reference. Never
  branch layout in C# or JS by viewport.
- Dates: native `<input type="date">`, no JS datepicker. Set `inputmode` and `type` per input
  kind.
- Touch targets ≥ 44 px below `lg`. Form action buttons sticky at the bottom.
  `env(safe-area-inset-bottom)` on fixed bottom elements. Tooltips are never the only carrier of
  information.
- No Bootstrap JS — use the TabBlazor services (`IModalService`, `IOffcanvasService`);
  unavoidable JS goes through an `IJSObjectReference` disposed in `IAsyncDisposable`.
- Custom CSS stays minimal, in `wwwroot/css/app.css`; look for a Tabler utility class first; no
  inline styles.

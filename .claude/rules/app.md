---
paths:
  - "src/App/**"
---

# Frontend

`EvilCase.App` is Blazor WebAssembly on TabBlazor, components over the Tabler CSS framework.

- `Icons/AppIcons.cs` holds only the icons the app uses, copied from the Tabler icon set. Never
  vendor the whole set.
- A new page goes inside `MainLayout`, which authenticates it. Placing one outside is an owner
  decision.

## Responsive

Desktop is primary. Mobile is first-class for the case list, case detail, deadlines and quick
notes; administrative flows only must not break.

- One breakpoint: `lg` (992 px), never mixed with `md`. Modals: `modal-fullscreen-lg-down`.
- Data lists never scroll horizontally on mobile: render a table and a card variant and switch
  by CSS only (`d-none d-lg-block` / `d-lg-none`); `Pages/Home.razor` is the reference. Never
  branch layout in C# or JS by viewport.
- Touch targets ≥ 44 px below `lg`. Form action buttons sticky at the bottom.
  `env(safe-area-inset-bottom)` on fixed bottom elements. A tooltip is never the only carrier of
  information.
- No Bootstrap JS — use the TabBlazor services (`IModalService`, `IOffcanvasService`);
  unavoidable JS goes through an `IJSObjectReference` disposed in `IAsyncDisposable`.
- Custom CSS stays minimal, in `wwwroot/css/app.css`. Look for a Tabler utility class first; no
  inline styles.

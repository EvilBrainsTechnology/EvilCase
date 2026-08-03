# Frontend

`EvilCase.App` is a Blazor WebAssembly app on [TabBlazor](https://github.com/TabBlazor/TabBlazor) (Blazor components over the Tabler CSS framework).

## TabBlazor and Tabler

- Services registered with `AddTabBlazor()` in `Program.cs`; `@using TabBlazor` in `_Imports.razor`. `index.html` must link `EvilBrains.EvilCase.App.styles.css` — several TabBlazor components (tooltip, dropdown, datepicker, popover, range slider) ship their CSS as Blazor scoped styles and silently render unstyled without it. The host emits a bundle of its own next to it; the app's is the one to link.
- Tabler CSS is vendored at `wwwroot/lib/tabler/tabler.min.css` (Tabler 1.4.0, matching the TabBlazor release) and popper.js 2.11.8 at `wwwroot/lib/popper/popper.min.js`, which `TablerOptions.PopperScriptUrl` points at. No CDN at build or runtime. Update by downloading the matching version.
- TabBlazor ships no icon set. `Icons/AppIcons.cs` holds only the icons the app uses; add one by copying its path data from the [Tabler icon set](https://tabler.io/icons) into a new `TablerIcon`. Do not vendor the whole generated set — the trimmer does not remove it.
- App shell: `Layout/MainLayout.razor` (Tabler `page` + `page-wrapper`) and `Layout/NavMenu.razor` — a single top bar holding the brand, the menu (from `lg` up) and the theme switch. Below `lg` the hamburger opens the navigation as an offcanvas via `IOffcanvasService`. Both render the same `Layout/NavLinks.razor`, which sets `active` on the `li` rather than through Blazor's `NavLink`: Tabler draws the indicator on `.nav-item.active`.
- Dark/light switch goes through `TablerService.SetTheme`. The initial theme follows `prefers-color-scheme`: an inline script in `index.html` applies it before Blazor boots and `ThemeSwitch` reads it back via `wwwroot/js/theme.js`. Changing an inline script changes its hash in the content security policy — see `src/Api/CLAUDE.md`.

A new page inside `MainLayout` is authenticated without doing anything; the browser half of authentication is in `src/Common/EvilCase.Auth/CLAUDE.md`.

`host.StartClientLogging()` must be called after `builder.Build()` in `Program.cs`. Forgetting it is silent: browser events buffer and are dropped.

## Responsive design

Desktop is the primary channel. Mobile must be fully usable for reading and quick flows; for administrative flows it only must not break.

- First-class on mobile: case list + search, case detail, deadlines, quick note.
- Must not break on mobile: bulk operations, long edit forms, user management, configuration.

Rules:

- The desktop/mobile breakpoint is `lg` (992px). Use it consistently; do not mix `md` and `lg` across pages.
- Data lists never scroll horizontally on mobile. Render both variants and switch them with CSS only — see `Pages/Home.razor` for the reference implementation:

  ```razor
  <div class="d-none d-lg-block">
      <QuickTable Items="@items">...</QuickTable>
  </div>

  <div class="d-lg-none">
      @foreach (var item in items) { <ItemCard Item="item" /> }
  </div>
  ```

  Both variants render; at 25–50 rows per page the cost is negligible.
- Never branch layout in C# by viewport. No JS interop for window width, no render branching on it.
- Modals: always `class="modal-fullscreen-lg-down"`, matching the `lg` breakpoint above.
- Dates: native `<input type="date">`, no JS datepicker.
- Keyboard: set `inputmode` and `type` per input kind (`numeric` for case numbers, `tel`, `email`).
- Touch targets: at least 44px for interactive elements below `lg`.
- Forms: action buttons (Save/Cancel) sticky at the bottom, not hidden below long content.
- Safe area: `env(safe-area-inset-bottom)` on fixed bottom elements.
- Tooltips are never the only carrier of information — touch has no hover.
- No Bootstrap JS components; they collide with the Blazor renderer. Use TabBlazor equivalents (`IModalService`, `IOffcanvasService`). Where JS is unavoidable, wrap it in an `IJSObjectReference` cleaned up in `IAsyncDisposable`.
- Custom CSS stays minimal and lives in `wwwroot/css/app.css`. Look for a Tabler utility class first. No inline styles.

# Layout & Navigation

Surfaces (accordion, card, panel), tabs, menus, top nav bar, error handler.

---

## Surfaces  (`…Components.Surfaces`)

### MaviCard  (`MaviComponentBase`)

Titled container card.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Title` | `string?` | — | Header text (base `Title`). |
| `ChildContent` | `RenderFragment?` | — | Body (padded). |
| `Width` | `string?` | `"w-96"` | Width class. |
| `ElementVariant` | `ElementVariant` | `Filled` | `Filled` (colored header/body), `Outlined`, `Text` (bordered light). |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Color. |

### MaviPanel  (`MaviComponentBase`)

Bordered panel with a floating "fieldset-legend" style title.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Title` | `string?` | — | Floating label at the top-left. |
| `ChildContent` | `RenderFragment?` | — | Body. |
| `ChildContentContainerCss` | `string?` | — | Classes for the body wrapper. |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Border/label color. |

### MaviAccordion + MaviAccordionItem

`MaviAccordion` owns expand/collapse state and cascades itself to its items.
`MaviAccordionItem` must be nested inside a `MaviAccordion`.

**`MaviAccordion`** (`MaviComponentBase`):

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `ChildContent` | `RenderFragment?` | — | The items. |
| `AllowMultiple` | `bool` | `false` | Allow several items open at once (default: single). |
| `Dividers` | `bool` | `true` | Thin dividers between items. |
| `ColorScheme` | `ThemeColorScheme` | `Default` | Header/detail tones (note: named `ColorScheme`, not `ThemeColorScheme`). |

**`MaviAccordionItem`** (`MaviComponentBase`):

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Title` | `string?` | — | Summary title (base `Title`). |
| `SubTitle` | `string?` | — | Secondary summary line. |
| `Summary` | `RenderFragment?` | — | Custom summary markup instead of Title/SubTitle. |
| `ChildContent` | `RenderFragment?` | — | Expanded body. |
| `Disabled` | `bool` | `false` | |
| `DefaultExpanded` | `bool` | `false` | Start open. |
| `ExpandedChanged` | `EventCallback<bool>` | — | Fires after toggle. |
| `IsFirst` / `IsLast` | `bool` | `false` | Position hints for rounded-stack styling. |

```razor
<MaviAccordion ColorScheme="ThemeColorScheme.Primary" AllowMultiple="false">
    <MaviAccordionItem Title="Shipping" DefaultExpanded="true">
        Shipping details…
    </MaviAccordionItem>
    <MaviAccordionItem Title="Billing" SubTitle="Card on file">
        Billing details…
    </MaviAccordionItem>
</MaviAccordion>
```

---

## Tabs  (`…Components.Tabs`)

Compose `MaviTabNavBar` (owns the shared `MaviTabContext`, cascaded) with
`MaviTab` buttons, and pair each tab with a `MaviTabContent` panel keyed by tab id.

**`MaviTabNavBar`** (`MaviComponentBase`):

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `ChildContent` *(required)* | `RenderFragment` | — | The `MaviTab`s (and optionally content). |
| `InitialActiveTabId` | `string?` | — | Tab selected on first render. |
| `ElementSize` | `ElementSize` | `Regular` | Cascaded to tabs. |
| `ThemeColorScheme` | `ThemeColorScheme` | `Default` | Cascaded accent. |
| `OnTabClick` | `EventCallback<TabClickEventArgs>` | — | Fires once per tab click with the selected tab id (parent re-renders don't add duplicate calls). |

**`MaviTab`** (`MaviComponentBase`): a tab button.

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `Title` | `string?` | — | Tab label. |
| `Icon` | `string?` | — | Leading icon CSS class. |
| `Disabled` | `bool` | `false` | |
| `ShowTitleOnBreakpoint` | `ScreenSizeBreakpoint` | (always) | Hide the title below this breakpoint (icon-only on small screens). |
| `Id` | `string?` | GUID | The tab's id — reference it from `MaviTabContent.BoundToTabId` and `InitialActiveTabId`. |

**`MaviTabContent`** (`MaviComponentBase`): shows its `ChildContent` only when its
tab is active.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `BoundToTabId` | `string?` | The `MaviTab.Id` this panel belongs to. |
| `Context` | `MaviTabContext?` | The nav bar's context (cascade or pass explicitly). |
| `ChildContent` | `RenderFragment?` | Panel content. |

```razor
<MaviTabNavBar InitialActiveTabId="tab-a" ThemeColorScheme="ThemeColorScheme.Primary">
    <MaviTab Id="tab-a" Title="Overview" Icon="lni lni-home-2" />
    <MaviTab Id="tab-b" Title="Details" />
</MaviTabNavBar>

<MaviTabContent BoundToTabId="tab-a" Context="_navBar.Context">Overview…</MaviTabContent>
```

> `MaviTabContext` (cascaded) carries `SelectedTabId`, `ElementSize`,
> `ThemeColorScheme`; clicking a tab sets `SelectedTabId` and raises `OnTabClicked`.

---

## Menus  (`…Components.Menus`)

### MaviMenuVertical  (`MenuBase` : `MaviComponentBase`)

Collapsible vertical sidebar menu. Items are pulled from a delegate and may be
flat items or expandable groups. Collapses/expands in response to a
`CircuitStateService` button event (`ElementConstants.Button.TOGGLE_SIDE_NAV_BAR`).

It injects `ICircuitStateService` (`Maviray.Blazor.Components.Core.Services`),
which `AddMaviComponents()` does **not** register; without it rendering throws
`Cannot provide a value for property 'CircuitStateService'`. Register it in each
project that renders the menu:

```csharp
builder.Services.AddScoped<ICircuitStateService, CircuitStateService>();
```

To toggle the menu from a button elsewhere (e.g. a hamburger in `TopNavBar`),
inject the same service and call
`ReportClick(ElementConstants.Button.TOGGLE_SIDE_NAV_BAR)` (`ElementConstants` is in
`Maviray.Blazor.Components.Core.Constants`).

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `PullMenuItems` | `Func<Task<IEnumerable<IMenuItem>>>?` | — | Async source of menu items. |
| `ElementSize` | `ElementSize` | `Regular` | Icon/label sizing. |
| `DarkMode` | `bool` | `false` | Dark palette. |
| `HighLightColorHex` | `string?` | — | Highlight color for the selected item (hex). |
| `ItemClicked` | `EventCallback<MenuItem>` | — | Fires when a (non-group) item is clicked. |

**Method:** `Task Refresh()` — re-pull items and re-render.

Item model (`…Components.Menus`): `MenuItem : MenuItemBase : IMenuItem` with
`Title, Icon, Key, Guid, Disabled, Selected, NavigateTo, RequireFullPageReload,
BadgeText, BadgeColor (ThemeColorScheme), Hidden`. `MenuItemGroup : MenuItemBase`
adds `Expanded` and `List<MenuItem> Items` for nested groups. Selection is tracked
via `MarkActive(guid)`.

### MaviDropDownMenu  (`MenuBase` : `MaviComponentBase`)

A button that opens a dropdown list of `MenuItem`s (closes on outside click via
JS interop).

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `MenuItems` *(required)* | `IEnumerable<MenuItem>?` | — | The items. |
| `Title` | `string?` | — | Trigger button text (base `Title`). |
| `Width` | `string` | `"w-48"` | Trigger width. |
| `DropDownWidth` | `string` | `"w-64"` | Menu width. |
| `DisplayIcons` | `bool` | `false` | Show the expand/collapse chevron. |
| `IconCollapsed` / `IconExpanded` | `string` | `lni lni-minus` / `lni lni-chevron-down` | Chevron icons. |
| `ShowIconAsTitle` | `bool` | `false` | Render an icon-only trigger button. |
| `IconCss` | `string?` | — | Extra classes for the icon-button trigger. |
| `TrimToLength` | `int` | 30 | Truncate item titles (with "…"). |
| `DropDownHorizontalAlightment` | `ElementHorizontalAlignment` | `Left` | Menu alignment. |
| `ItemClicked` | `EventCallback<MenuItem>` | — | Fires with the clicked item. |

```razor
<MaviDropDownMenu Title="Actions" MenuItems="_actions" ItemClicked="OnAction" />
```

---

## TopNavBar  (`…Components.NavBars`, `NavBarBase` : `MaviComponentBase`)

A three-slot top app bar (left / center / right).

| Parameter | Type | Purpose |
|-----------|------|---------|
| `LeftContent` / `CenterContent` / `RightContent` | `RenderFragment?` | The three regions (space-between layout). |
| `Class`, `Style`, `Id` | | As base. |

```razor
<TopNavBar>
    <LeftContent><span class="font-heading">MyApp</span></LeftContent>
    <RightContent><MaviButtonIcon Icon="lni lni-user-4" /></RightContent>
</TopNavBar>
```

---

## MaviErrorHandler  (`…Components.ErrorHandlers`, inherits `ErrorBoundary`)

A root error boundary. Wrap the app's routed content in it: on an unhandled
exception it logs the error and shows a friendly message plus a **Home** button
that recovers and navigates to `/`. Use in the layout/`Routes` around
`@Body`/`RouteView`.

| Member | Purpose |
|--------|---------|
| `ChildContent` | Content to guard (inherited from `ErrorBoundary`). |
| `RecoverAndRedirectToHome()` | Recovers the boundary and navigates home (wired to the Home button). |

```razor
<MaviErrorHandler>
    <Router AppAssembly="typeof(Program).Assembly">…</Router>
</MaviErrorHandler>
```

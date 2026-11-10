---
name: mavi-grid
description: >-
  Use when adding, configuring, or troubleshooting a data grid / table in a
  .NET/Blazor app that consumes the Maviray grid packages (the `MaviGrid<TKey>`
  component and `IGridDataFactory<TDataItem, TModel, TKey>`). Covers server-side
  paging, sorting and filtering over large tables, defining columns from a model
  with `[GridDataProperty]` attributes, `GridFetchOptions` (includes, path maps,
  badge colours), the EF Core data source, access policies (deny / row filters),
  fetch observers (audit), global filters, row & header menus, selection, fetch
  error / access-denied / prerender states, and DI/host wiring. Reach for this
  whenever MaviGrid, a "Mavi" grid/table, paged tables, grid authorization, or
  Maviray.Blazor.Components.Pro.Grid is involved, even if the request just says
  "add a table/grid". Self-contained; the library ships as NuGet packages.
---

# MaviGrid (Maviray Blazor Pro Grid — consumer)

## Overview

`MaviGrid<TKey>` is a server-paged Blazor grid. You never write columns or `<tr>`s: columns are attributes on a grid
model class, and the grid asks for exactly one page at a time through `FetchData(query, cancellationToken)`, which you
pass to `IGridDataFactory.BuildPageAsync`.

```
MaviGrid ──(MaviGridQuery, ct)──► FetchData ──► IGridDataFactory.BuildPageAsync(query, options, ct)
  1. access policies          deny → column-only page with AccessDenied = true, no database call
  2. one combined filter      column filters ∧ options.Filter ∧ global filters ∧ policy row filters
  3. IGridDataSource.CountAsync, then GetPageAsync (only the page; skipped when count = 0)
  4. Mapster entity→model, model.ConstructFrom(entity), format cells (cached metadata)
  5. fetch observers (row keys)
◄── MaviGridCollection<TKey>
```

The EF source orders by your sort, then the primary key (stable paging), uses `OFFSET/FETCH` and no tracking, and sends
filter values as parameters. The source isn't on disk in a consuming app; use these tables and IntelliSense.

## Namespaces

| Namespace | Types |
|---|---|
| `Maviray.Blazor.Components.Pro.Grid.UI.Components` | `MaviGrid<TKey>`, `MaviGridParameters` |
| `Maviray.Blazor.Components.Pro.Core.Models.Grid` | `MaviGridQuery`, `MaviGridColumnFilter`, `MaviGridCollection<TKey>`, `MaviGridColumn`, `MaviGridRow<TKey>`, `MaviGridCell`, `GridFetchOptions<TDataItem, TModel>`, `GridPropertyPathMap<TDataItem, TModel>` |
| `Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu` | `MaviRowContextMenuItem<TKey>`, `CellClickParameters<TKey>` |
| `Maviray.Blazor.Components.Pro.Core.Models` | `MaviContextMenuItem` |
| `Maviray.Blazor.Components.Pro.Core.Interfaces` | `IGridCollectionModel<TDataItem, TKey>`, `IMaviContextMenuItem` |
| `Maviray.Blazor.Components.Pro.Core.Interfaces.Grid` | `IGridDataFactory<,,>`, `IGridDataSource<T>`, `GridDataSourceQuery<T>`, `GridSort`, `IGridGlobalFilterProvider`, `IMaviRowContextMenuItem<TKey>` |
| `Maviray.Blazor.Components.Pro.Core.Access` | `IGridAccessPolicy`, `GridAccessPolicy<TEntity>`, `GridAccessDecision<T>`, `GridAccessRequest`, `IGridFetchObserver`, `GridRowsLoaded<TKey>`, `GridAccessDenied`, `GridFilterExpression` |
| `Maviray.Blazor.Components.Pro.Core.Attributes.Grid` | the column attributes |
| `Maviray.Blazor.Components.Pro.Core.Extensions` | `AddMaviGrid`, `MaviGridBuilder`, `OrderByDynamic` / `ThenByDynamic` |
| `Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions` | `AddEntityFrameworkDataSource<TDbContext>()` |

## Quick start

```csharp
// Program.cs
builder.Services.AddMaviComponents();
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(cs));   // a FACTORY, not AddDbContext
builder.Services.AddMaviGrid()                                                 // options: o => o.RequireExplicitAllow / MaxPageSize
    .AddEntityFrameworkDataSource<AppDbContext>();
```

```csharp
public class OrderGridModel : IGridCollectionModel<Order, int>
{
    [GridDataProperty(1)] public int Id { get; set; }
    [Display(Name = "Customer")] [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)] public string? CustomerName { get; set; }
    [GridDataProperty(3)] [GridAdvancedDisplay] public OrderStatus Status { get; set; }
    public int Key => Id;
    public void ConstructFrom(Order o) => CustomerName = o.Customer?.Name;   // same-named members are auto-mapped
    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() =>
        [new MaviRowContextMenuItem<int> { Key = Id, Title = "Edit", Icon = "lni lni-pencil-1" }];
}
```

```razor
<MaviGrid @ref="_grid" TKey="int" FetchData="FetchPageAsync" Parameters="_parameters"
          OnRowContextMenuClick="OnRowAction" OnCellClick="OnCellClick" />

@code {
    // Once per grid (static/field), not per fetch: the factory caches columns per options/path map.
    private static readonly GridFetchOptions<Order, OrderGridModel> Options = new()
    {
        Include = q => q.Include(o => o.Customer),
        PropertyPathMap = new GridPropertyPathMap<Order, OrderGridModel>().Map(m => m.CustomerName, "Customer.Name"),
        CellThemes = new Dictionary<string, Func<object?, ThemeColorScheme>>
        {
            ["Status"] = v => v is OrderStatus.Late ? ThemeColorScheme.Alert : ThemeColorScheme.Success
        }
    };

    private readonly MaviGridParameters _parameters = new() { EnableFilters = true, SortColumnKey = "Id" };
    private MaviGrid<int>? _grid;

    [Inject] private IGridDataFactory<Order, OrderGridModel, int> Grid { get; set; } = default!;

    private Task<MaviGridCollection<int>> FetchPageAsync(MaviGridQuery query, CancellationToken ct) =>
        Grid.BuildPageAsync(query, Options, ct);
}
```

Always set `TKey="…"` explicitly: Razor can't infer it through method-group event callbacks.

## Columns

| Attribute | Effect |
|---|---|
| `[GridDataProperty(sequence, behavior = Default)]` | column; ordered by sequence, then declaration; `Navigational` = bold, clickable |
| `[Display(Name = "…")]` | header text (and enum member text) |
| `[GridNavProperty(sequence)]` on a related-model property | eager-loads that navigation (same name on the entity); its `[GridNestedProperty("Title")]` properties become columns keyed `Nav.Prop` |
| `[GridBooleanDisplayValues("Yes", "No")]` | bool text, also what the filter matches |
| `[GridDateTimeFormat("yyyy-MM-dd")]` | date/time format (invariant) |
| `[GridAdvancedDisplay]` | badge; colour from `GridFetchOptions.CellThemes[columnKey](rawValue)` |

**Sortable / filterable:** only when the column resolves to an entity path, either an identically named member or a
`GridPropertyPathMap` entry (`Map(m => m.X, "Nav.Path", nullDefault: false)` for `?? false` in `ConstructFrom`).
Computed columns without a path show no sort button and no filter box. The factory ignores sort and filter keys that
aren't columns. To constrain on a non-column property, use `GridFetchOptions.Filter`, a global filter or a policy.

## `MaviGrid<TKey>`

| Parameter | Purpose |
|---|---|
| `FetchData` (required) | `Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<TKey>>>`; pass the token on |
| `Parameters` | `MaviGridParameters` (below) |
| `ConstructMainContextMenu` | header ⋯ menu; built at start and on `RefreshAsync()` |
| `OnMainContextMenuButtonClick`, `OnRowContextMenuClick`, `OnCellClick` | user actions; exceptions you throw reach your error boundary |
| `OnFetchError` | `EventCallback<Exception>`, a failed fetch (grid-initiated cancellations are not errors) |
| `NoDataContent`, `AccessDeniedContent`, `FetchErrorContent`, `PrerenderContent` | replace the built-in states |

Methods: `RefreshAsync()`, `SelectedKeys` / `ClearSelection()` (kept across pages), `SetColumnVisible(key, bool)` /
`HiddenColumnKeys` (kept across pages; users also have a column dialog in the pager).

| `MaviGridParameters` | Default | Effect |
|---|---|---|
| `SortColumnKey`, `SortOrder` | none, `Ascending` | initial sort (applied once; the user's choice wins after that) |
| `PageSize`, `PageSizeOptions` | `10`, `[10,25,50,100]` | initial size and pager choices; the factory caps at `MaxPageSize` (100) and the pager shows the size applied |
| `EnableFilters`, `FilterDebounceMilliseconds` | `false`, `400` | filter row; text: substring for strings, equality for numbers/dates/ids, label match for bools/enums |
| `DisplaySelectColumn` | `false` | row checkboxes |
| `TableRowContextMenuDisplayStyle` | `DropDown` | `Icons` shows row actions inline |
| `Striped`, `Condensed`, `MaxCellCharsToDisplay` | `false`, `false`, `0` | look |
| `NoDataAvailableTitle`, `AccessDeniedTitle`, `FetchErrorTitle` | built-in texts | state texts |
| `FetchWhilePrerendering` | `true` | `false`: placeholder while prerendering, one fetch once interactive |

States (`data-grid-state`): `prerender`, `fetch-error` (with Retry), `access-denied` (header and ⋯ menu kept; no
filters, select column or pager), `no-data`.

## Access policies

```csharp
public sealed class OrderPolicy(AuthenticationStateProvider auth) : GridAccessPolicy<Order>   // NotApplicable for other types
{
    protected override async ValueTask<GridAccessDecision<Order>> EvaluateAsync(GridAccessRequest r, CancellationToken ct)
    {
        var user = (await auth.GetAuthenticationStateAsync()).User;
        if (!user.IsInRole("Sales")) return GridAccessDecision<Order>.Deny("Only sales can see orders.");
        var region = user.FindFirst("region")?.Value;
        return GridAccessDecision<Order>.Allow(o => o.Region == region);   // limits count and page
    }
}
// .AddAccessPolicy<OrderPolicy>()   (scoped; registering the same type twice adds it once)
```

- Outcomes: `NotApplicable()`, `Allow(rowFilter?)` (filters of all allows are AND-combined), `Deny(message?)` (the
  first deny wins; no database call).
- A throw or a `null` decision fails the fetch, never allows it.
- `AddMaviGrid(o => o.RequireExplicitAllow = true)` denies every fetch no policy allows (fail-closed).
- Cross-cutting rules over an interface implement `IGridAccessPolicy` (generic method) and use
  `(Expression<Func<TDataItem,bool>>)GridFilterExpression.ForInterface(typeof(TDataItem), (Expression<Func<ITenantScoped,bool>>)(x => x.TenantId == id))`
  after checking `typeof(ITenantScoped).IsAssignableFrom(typeof(TDataItem))`. Explicit interface implementations throw.
- `GridFetchOptions.AccessTag` distinguishes grids over the same type (a user list and an admin list).
- In Blazor Server a scope is the circuit:
  - read the user from `AuthenticationStateProvider`;
  - use `IDbContextFactory`, not a scoped `DbContext` (grids fetch concurrently);
  - don't cache grants for the whole circuit; use a short expiry.

## Fetch observers (audit)

`IGridFetchObserver` has two optional members:

- `OnRowsLoadedAsync<TDataItem, TKey>(GridRowsLoaded<TKey>)` gets the row keys in display order, the page, the size
  and the total. It is called for empty pages too.
- `OnAccessDeniedAsync<TDataItem>(GridAccessDenied)` is called for every denied fetch.

Observers run inside the fetch, and their exceptions fail it. For best-effort audit, catch and queue to a background
writer. Set `FetchWhilePrerendering = false` or every view is recorded twice. Register with `.AddFetchObserver<T>()`.

## Global filters

`IGridGlobalFilterProvider.GetFilter<TDataItem>()` returns a synchronous predicate for every grid, or `null` when the
type isn't its concern. Use it for soft-delete style constraints, never for granting access. Register with
`.AddGlobalFilter<T>()`, or `.AddGlobalFilter(instance)` for a singleton.

## Custom data source

Implement `IGridDataSource<T>`:

- `CountAsync(query, ct)` counts rows matching `query.Filter`;
- `GetPageAsync(query, ct)` applies `IncludePaths`, `Include`, `Filter` and `Sort`, then `Skip`/`Take`, and returns a
  list. With `Sort == null`, order by the key;
- keep it stateless, and order deterministically (tie-break with a unique key; `OrderByDynamic(...).ThenByDynamic("Id", order)`);
- register it for one type (`services.AddScoped<IGridDataSource<Order>, OrderSource>()`), or for all types
  (`.AddDataSource(typeof(MySource<>))`).

## Gotchas

| Do | Don't |
|---|---|
| Create `GridFetchOptions` once per grid | build options, maps or theme dictionaries inside `FetchData` |
| Pass `FetchData`'s token to `BuildPageAsync` | drop it; superseded fetches then run to the end |
| Give every column `[GridDataProperty]` | expect unmarked properties to show |
| Map computed columns with `GridPropertyPathMap` to make them sortable | expect a computed column to sort without a path |
| Eager-load related data (`Include` or `[GridNavProperty]`) before reading it in `ConstructFrom` | rely on lazy loading; contexts are closed after each query |
| Use `AddDbContextFactory` / `AddPooledDbContextFactory` | register only a scoped `DbContext` |
| Return `NotApplicable` for types a policy doesn't govern | return `Deny` by default; it blocks every grid |
| Set `TKey="int"` on `<MaviGrid>` | rely on type inference with method-group callbacks |
| Call `_grid.RefreshAsync()` after edits | expect the grid to notice changes on its own |

## Host setup

```html
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Pro.Grid.UI/css/maviray.grid.min.css" />
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Material/css/maviray.material.min.css" />
<link rel="stylesheet" href="https://cdn.lineicons.com/5.0/lineicons.css" />
<script src="_content/Maviray.Blazor.Components.Material/js/maviJsInterop.js"></script>  <!-- menu outside-click -->
```

Use an interactive render mode. See the `material-css-styling` skill for Tailwind work around the grid.

## Pointers

The installed version is in the app's `<PackageReference Include="Maviray.Blazor.Components.Pro.Grid.UI" …/>`. With a
local `blazor-ui-kit-pro` checkout, the ground truth is:

- `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor(.cs)`;
- `src/Maviray.Blazor.Components.Pro.Core/Services/GridDataFactory.cs` and `.../Access/**`;
- `src/Maviray.Blazor.Components.Pro.Grid.EntityFramework/Services/EntityFrameworkGridDataSource.cs`;
- the sample pages `Home.razor` (menus, badges, selection), `PropertyMapping.razor` (path map, includes),
  `Scoped.razor` (global filter) and `Access.razor` (policy, observer).

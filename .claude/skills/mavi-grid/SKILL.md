---
name: mavi-grid
description: >-
  Use when adding, configuring, or troubleshooting a data grid / table in a
  .NET/Blazor app that consumes the Maviray grid packages (the `MaviGrid<TKey>`
  component and `IGridDataFactory<TDataItem, TModel, TKey>`). Covers server-side
  paging, sorting and filtering over large tables, defining columns from a model
  with `[GridDataProperty]` attributes, `GridFetchOptions` (includes, path maps,
  badge colours), the EF Core data source, access policies (deny / row filters /
  hidden columns), loading only the shown columns (`GridProjection`), reusing the
  prerendered page (`PrerenderedPageKey`),
  fetch observers (audit), global filters, row & header menus, selection, fetch
  error / access-denied / prerender states, fast paging (count reuse, keyset
  cursors), feeding the grid from your own service or DTOs (`GridPageBuilder`,
  `GridKeyset`), and DI/host wiring. Reach for this
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
  3. IGridDataSource.CountAsync (skipped when query.KnownTotalRows is set), then only the page
     (skipped when count = 0; sought from query.Cursor instead of skipped to, with a keyset data source)
  4. Mapster entity→model, model.ConstructFrom(entity), format cells (cached metadata)
  5. fetch observers (row keys)
◄── MaviGridCollection<TKey>   (with NextPageCursor / PreviousPageCursor for keyset paging)
```

The EF source orders by your sort, then the primary key (stable paging), uses no tracking, and sends filter values as
parameters. A next/previous click with the same sort and filters costs one query: no count (the grid sends the total
it has) and no offset (it seeks from the cursor of the edge row). The source isn't on disk in a consuming app; use
these tables and IntelliSense.

## Namespaces

| Namespace | Types |
|---|---|
| `Maviray.Blazor.Components.Pro.Grid.UI.Components` | `MaviGrid<TKey>`, `MaviGridParameters` |
| `Maviray.Blazor.Components.Pro.Core.Models.Grid` | `MaviGridQuery`, `MaviGridColumnFilter`, `MaviGridCursor`, `MaviGridCollection<TKey>`, `MaviGridColumn`, `MaviGridRow<TKey>`, `MaviGridCell`, `GridFetchOptions<TDataItem, TModel>`, `GridPropertyPathMap<TDataItem, TModel>`, `GridProjection<TDataItem>`, `GridLoadedPage<T>`, `GridKeysetPage<T>` |
| `Maviray.Blazor.Components.Pro.Core.Services` | `GridPageBuilder<TDataItem, TModel, TKey>` |
| `Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu` | `MaviRowContextMenuItem<TKey>`, `CellClickParameters<TKey>` |
| `Maviray.Blazor.Components.Pro.Core.Models` | `MaviContextMenuItem` |
| `Maviray.Blazor.Components.Pro.Core.Interfaces` | `IGridCollectionModel<TDataItem, TKey>`, `IMaviContextMenuItem` |
| `Maviray.Blazor.Components.Pro.Core.Interfaces.Grid` | `IGridDataFactory<,,>`, `IGridDataSource<T>`, `IGridKeysetDataSource<T>`, `GridDataSourceQuery<T>`, `GridSort`, `IGridGlobalFilterProvider`, `IMaviRowContextMenuItem<TKey>` |
| `Maviray.Blazor.Components.Pro.Core.Access` | `IGridAccessPolicy`, `GridAccessPolicy<TEntity>`, `GridAccessDecision<T>`, `GridAccessRequest`, `IGridFetchObserver`, `GridRowsLoaded<TKey>`, `GridAccessDenied`, `GridFilterExpression` |
| `Maviray.Blazor.Components.Pro.Core.Attributes.Grid` | the column attributes |
| `Maviray.Blazor.Components.Pro.Core.Extensions` | `AddMaviGrid`, `MaviGridBuilder`, `OrderByDynamic` / `ThenByDynamic` / `SelectPaths`, `GridKeyset<T>`, `GridOrderKey` |
| `Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions` | `AddEntityFrameworkDataSource<TDbContext>()` |

## Quick start

```csharp
// Program.cs
builder.Services.AddMaviComponents();
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(cs));   // a FACTORY, not AddDbContext
builder.Services.AddMaviGrid()                                                 // options: o => o.RequireExplicitAllow / MaxPageSize / ReuseTotalRows (validated at startup)
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

**Loading only what's shown (wide tables):** `GridFetchOptions.Projection = new GridProjection<Order>().Load(o => o.Approved)`
makes each page load just the visible columns' paths plus the members you list, into new `Order` instances (related
items too: `Customer.Name` loads only that member of `Customer`; a missing customer stays `null`). No `Include` is
needed for the column paths. List every member `ConstructFrom` and `GetContextActions` read that isn't a column path:
an unlisted one reads as its default value. Entity and related types need a parameterless constructor (any access)
and settable members.

## `MaviGrid<TKey>`

| Parameter | Purpose |
|---|---|
| `FetchData` (required) | `Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<TKey>>>`; pass the token on |
| `Parameters` | `MaviGridParameters` (below) |
| `ConstructMainContextMenu` | header ⋯ menu; built at start and on `RefreshAsync()` |
| `OnMainContextMenuButtonClick`, `OnRowContextMenuClick`, `OnCellClick` | user actions; exceptions you throw reach your error boundary |
| `OnFetchError` | `EventCallback<Exception>`, a failed fetch (grid-initiated cancellations are not errors) |
| `NoDataContent`, `AccessDeniedContent`, `FetchErrorContent`, `PrerenderContent` | replace the built-in states |
| `PrerenderedPageKey` | a key unique among the page's grids: the page fetched while prerendering is stored (`PersistentComponentState`) and the interactive grid starts from it, so a view costs one query and one audit entry and the rows show at once. Reused only for the same initial page, size, sort and filters; restored row actions are `MaviRowContextMenuItem<TKey>` |

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
| `FetchWhilePrerendering` | `true` | `false`: placeholder while prerendering, one fetch once interactive (or keep `true` and set `PrerenderedPageKey`: rows in the prerendered HTML, still one fetch) |

States (`data-grid-state`): `prerender`, `fetch-error` (with Retry), `access-denied` (header and ⋯ menu kept; no
filters, select column or pager), `no-data`. A filter value the column's type can't hold (`99999999999` for an `int`)
matches no rows rather than failing the fetch.

**Paging hints in `MaviGridQuery`** (fetch code may use or ignore them):

- `KnownTotalRows`: the current page's total, sent when only the page, page size or sort changed. `null` on the first
  fetch, after a filter change, on `RefreshAsync()` and after a denied or failed fetch.
- `Cursor` (`MaviGridCursor(Value, Direction)`): the current page's `NextPageCursor` / `PreviousPageCursor`, sent when
  moving to the adjacent page (not page 1) with the same sort, filters and page size. `PageNumber` is still the target.

## Fetching through your own service (DTOs)

When an app's service layer loads pages itself (DTOs behind its own authorization, so no `IGridDataFactory`), keep
`MaviGrid` and build the page with `GridPageBuilder`. `TDataItem` is the DTO; `TRow : IGridCollectionModel<TDto, TKey>`
carries the column attributes (private setters filled in `ConstructFrom` are fine).

```csharp
private async Task<MaviGridCollection<Guid>> FetchPageAsync(MaviGridQuery query, CancellationToken ct)
{
    var result = await Customers.GetPageAsync(ToRequest(query), ct);          // your service; it can honour the hints
    if (result.IsForbidden) return GridPageBuilder<CustomerDto, CustomerRow, Guid>.Denied("You can't view customers.");

    var page = result.Value;
    return GridPageBuilder<CustomerDto, CustomerRow, Guid>.Build(
        new GridLoadedPage<CustomerDto>(page.Items, page.Page, page.PageSize, page.TotalCount)
        {
            NextPageCursor = page.NextCursor, PreviousPageCursor = page.PreviousCursor,  // optional, for keyset
            HiddenColumns = page.HiddenColumns                                           // optional, column access
        },
        query);
}
```

- `GridPageBuilder<…>.Columns(map, hiddenColumns)` lists the columns; those with `Sortable`/`Filterable` (a same-named
  DTO member, or a path-map entry) are the only sort and filter keys your query code must translate. Others never come
  from the grid. Hidden columns are left out of the page; ignore sort and filter keys for them in your query code too.
- **Skip the count:** when `query.KnownTotalRows` is set, use it, but if the loaded page holds fewer rows than that
  total predicts (rows were deleted), count again and clamp the page.
- **Seek instead of skipping:** in the repository, after the scope and filters,
  `new GridKeyset<Customer>([new GridOrderKey(sortColumn, order), new GridOrderKey("Number", order)]).LoadPageAsync(filtered, query.Cursor, skip, take, (q, ct) => q.ToListAsync(ct), ct)`
  returns the items and the first/last row cursors. End the keys with a unique column (a `Guid` primary key is fine:
  the database compares it in its own order). Keys must be non-null numbers, strings, bools, Guids, enums (stored as
  numbers), dates or times (`GridKeyset<T>.CanSeek`); give a nullable key a null default. The load delegate may project
  (`q.SelectPaths(paths)` or your own `Select`) as long as the order keys stay loaded. A cursor from another order is
  ignored (it skips instead).

## Access policies

```csharp
public sealed class OrderPolicy(AuthenticationStateProvider auth) : GridAccessPolicy<Order>   // Order and its subtypes; NotApplicable for other types
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
- **Column access:** `Allow(o => …).WithHiddenColumns("Margin", "Customer.Phone")` hides columns by key (model property
  name, or `Nav.Prop`). Hidden columns are removed from the page (header, cells, column dialog), their sort and filter
  keys are ignored so values can't be guessed by filtering, and a projection doesn't load them. A column-only policy
  returns `NotApplicable().WithHiddenColumns(…)`, which hides without allowing rows. Every policy's hidden columns
  are combined.
- A throw or a `null` decision fails the fetch, never allows it.
- `AddMaviGrid(o => o.RequireExplicitAllow = true)` denies every fetch no policy allows (fail-closed).
- `GridAccessPolicy<TEntity>` covers types derived from `TEntity` (EF inheritance), with the row filter rebound to the
  subtype, so a grid over `RushOrder : Order` gets `OrderPolicy`'s decision. A policy for the subtype doesn't cover
  the base type.
- Cross-cutting rules over an interface: `GridAccessPolicy<ITenantScoped>` covers every type that implements it
  (`Allow(x => x.TenantId == id)` is rebound to each entity's own property, so EF translates it). For full control,
  implement `IGridAccessPolicy` (generic method) and use `GridFilterExpression.ForInterface`. Explicit interface
  implementations throw.
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
writer. Set `PrerenderedPageKey` (or `FetchWhilePrerendering = false`) or every view is recorded twice. Register with
`.AddFetchObserver<T>()`.

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
  (`.AddDataSource(typeof(MySource<>))`);
- when `query.SelectPaths` is set (a grid with a `Projection`), skip the includes and finish the page query with
  `.SelectPaths(query.SelectPaths)`, adding your key and order paths if you read cursors;
- for keyset paging, implement `IGridKeysetDataSource<T>` too: `GetKeysetPageAsync` returns
  `GridKeyset<T>.LoadPageAsync(filtered, query.Cursor, query.Skip, query.Take, …)` (the same order as `GetPageAsync`),
  and the factory then hands cursors to the grid.

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
| Call `_grid.RefreshAsync()` after edits, and when a user's access changes | expect the grid to notice changes on its own; it reuses the total across page clicks |

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

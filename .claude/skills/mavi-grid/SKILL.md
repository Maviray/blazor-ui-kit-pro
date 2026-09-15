---
name: mavi-grid
description: >-
  Use when adding, configuring, or troubleshooting a data grid / table in a
  .NET/Blazor app that consumes the Maviray grid packages (the generic
  `MaviGrid<TDataItem, TModel, TKey>` component). Covers server-side paging,
  sorting and filtering, defining columns from a model with `[GridDataProperty]`
  attributes, the `IGenericGridDataFactory` data pipeline, EF Core data sources,
  global/scoped filters, row & header context menus, badge columns, and DI/host
  wiring. Reach for this whenever MaviGrid, a "Mavi" grid/table, paged tables, or
  Maviray.Blazor.Components.Pro.Grid is involved, even if the request just says
  "add a table/grid". Self-contained; the library ships as NuGet packages.
---

# MaviGrid (Maviray Blazor Pro Grid — consumer)

## Overview

`MaviGrid` is a generic, server-paged Blazor data grid. You **never hand-write
columns or `<tr>`s**: columns are declared by attributes on a *grid model* class,
and the grid asks you for one page of data at a time through a `FetchData`
delegate. A `IGenericGridDataFactory` turns query parameters (page, sort, filters)
into an `IQueryable` against a pluggable **data source** (EF Core out of the box),
materializes each row into your model, and returns a `MaviGridCollection`.

Data flow for every page / sort / filter / page-size change:

```
MaviGrid  ──(MaviGridDataQueryParameters)──►  your FetchData delegate
  your FetchData  ──►  IGenericGridDataFactory.BuildDataCollection(...)
    factory  ──►  IGridDataSource<TDataItem>  (EF Core: Count + paged Get)
    factory  ──►  Mapster auto-map entity→model, then model.ConstructFrom(entity)
    factory  ──►  reflect [GridDataProperty] attributes into columns + cells
  ◄── MaviGridCollection<TKey>  (columns, rows, totals, current page)
```

Because the library is distributed as NuGet packages, its source is **not
readable on disk** in a consuming app — rely on the tables below and IDE
IntelliSense. If a local checkout of `blazor-ui-kit-pro` is available as an added
directory, see **Pointers** for the files that are the ground truth.

## When to Use

- Adding any paged/sortable/filterable table or grid to a page
- Defining which entity columns appear, their order, headers, and formatting
- Wiring the data factory, an EF Core (or custom) data source, and DI
- Adding row actions, a header action menu, cell-click navigation, row selection
- Showing related-entity columns, computed columns, or badge/status columns
- Enforcing a cross-cutting row constraint (soft-delete, tenant, permission scope)

## Packages & Namespaces

| Package | Provides |
|---|---|
| `Maviray.Blazor.Components.Pro.Grid.UI` | the `MaviGrid` component + `MaviGridParameters` |
| `Maviray.Blazor.Components.Pro.Core` | model interface, `[Grid*]` attributes, models, enums, `IGenericGridDataFactory`, DI extensions |
| `Maviray.Blazor.Components.Pro.Grid.EntityFramework` | `EntityFrameworkGridDataSource<TDataItem,TDbContext>` (optional — or implement `IGridDataSource<T>` yourself) |

Pulls in `Maviray.Blazor.Components.Material` and `...Core` transitively (see the
`material-css-styling` skill for styling those). Common `@using`s in a page:

```razor
@using Maviray.Blazor.Components.Core.Enums                      @* SortOrder, ThemeColorScheme, ElementVariant, ElementSize, TableRowContextMenuDisplayStyle *@
@using Maviray.Blazor.Components.Pro.Core.Interfaces            @* IGridCollectionModel, IMaviContextMenuItem *@
@using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid       @* IGenericGridDataFactory, IMaviRowContextMenuItem *@
@using Maviray.Blazor.Components.Pro.Core.Models.Grid           @* MaviGridCollection, MaviGridColumn, GridPropertyPathMap, MaviGridCell *@
@using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter    @* MaviGridDataQueryParameters *@
@using Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu      @* MaviContextMenuItem, MaviRowContextMenuItem, CellClickParameters *@
@using Maviray.Blazor.Components.Pro.Core.Attributes.Grid       @* [GridDataProperty], [GridNavProperty], ... (on the model) *@
@using Maviray.Blazor.Components.Pro.Core.Enums.Grid            @* MaviGridColumnCellBehavior, MaviGridFilterCriteria *@
@using Maviray.Blazor.Components.Pro.Grid.UI.Components          @* MaviGrid, MaviGridParameters *@
```

## Quick Start (end to end)

**1 — Grid model** (implements `IGridCollectionModel<TEntity, TKey>`; attributes define columns):

```csharp
public class OrderGridModel : IGridCollectionModel<Order, int>
{
    [Display(Name = "Id")]          [GridDataProperty(1)] public int Id { get; set; }
    [Display(Name = "Number")]      [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)] public string? OrderNumber { get; set; }
    [Display(Name = "Placed")]      [GridDataProperty(3)] [GridDateTimeFormat("yyyy-MM-dd")] public DateTime PlacedOn { get; set; }
    [Display(Name = "Paid")]        [GridDataProperty(4)] [GridBooleanDisplayValues("Yes", "No")] public bool IsPaid { get; set; }
    [Display(Name = "Total")]       [GridDataProperty(5)] public decimal Total { get; set; }

    public int Key => Id;                                   // row key (→ TKey)
    public void ConstructFrom(Order entity) { }            // fill computed/related props (see below)
    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];  // per-row action buttons
}
```

**2 — Page**:

```razor
@page "/orders"
<MaviGrid @ref="_grid"
          TDataItem="Order" TModel="OrderGridModel" TKey="int"
          FetchData="FetchGridData"
          Parameters="_params" />

@code {
    private readonly MaviGridParameters _params = new() { EnableFilters = true, DisplaySelectColumn = true };
    private MaviGrid<Order, OrderGridModel, int>? _grid;

    [Inject] private IGenericGridDataFactory<Order, OrderGridModel, int>? DataFactory { get; set; }

    private async Task<MaviGridCollection<int>?> FetchGridData(MaviGridDataQueryParameters p)
    {
        if (string.IsNullOrWhiteSpace(p.OrderByColumnKey)) p.OrderByColumnKey = "Id";   // initial sort column
        return DataFactory is null ? null : await DataFactory.BuildDataCollection(p);
    }
}
```

**3 — DI + host assets**: see **Dependency Injection** and **Host Setup** below.

`FetchData` is the one required parameter — without it the grid renders its empty
state. Everything else (columns, paging, sorting, filtering, totals) is handled
for you.

## Component API — `MaviGrid<TDataItem, TModel, TKey>`

**Type parameters** (constraints enforced by the compiler):

| Param | Constraint | Meaning |
|---|---|---|
| `TDataItem` | `class` | the source entity (e.g. an EF Core entity) |
| `TModel` | `class, IGridCollectionModel<TDataItem, TKey>` | the grid/view model whose attributes define columns |
| `TKey` | `IEquatable<TKey>` | row key type (e.g. `int`, `Guid`) |

**Parameters**:

| Parameter | Type | Purpose |
|---|---|---|
| `FetchData` **(required)** | `Func<MaviGridDataQueryParameters, Task<MaviGridCollection<TKey>?>>` | return one page; normally delegates to the factory |
| `Parameters` | `MaviGridParameters` | appearance & behavior (defaults to `new()`) |
| `ConstructMainContextMenu` | `Func<Task<IEnumerable<IMaviContextMenuItem>>>` | builds the header-level action menu (⋯) |
| `OnMainContextMenuButtonClick` | `EventCallback<IMaviContextMenuItem>` | a header menu item was clicked |
| `OnRowContextMenuClick` | `EventCallback<IMaviRowContextMenuItem<TKey>>` | a row action was clicked |
| `OnCellClick` | `EventCallback<CellClickParameters<TKey>>` | a cell was clicked (`RowKey`, `ColumnKey`, `CellBehavior`) |
| `NoDataContent` | `RenderFragment` | custom empty-state markup |

**Public methods** (call via `@ref`):

| Method | Use |
|---|---|
| `Task Refresh()` | re-run `FetchData` and re-render (e.g. after an edit/delete) |
| `IEnumerable<MaviGridRow<TKey>> GetSelectedRows()` | rows ticked in the select column (needs `DisplaySelectColumn = true`); each exposes `Key` |

## `MaviGridParameters` (appearance & behavior)

Inherits most options from `TableParameters`. Defaults shown.

| Property | Type | Default | Effect |
|---|---|---|---|
| `EnableFilters` | `bool` | `false` | show the per-column filter input row |
| `DisplaySelectColumn` | `bool` | `false` | show a row-checkbox column (`GetSelectedRows()`) |
| `Striped` | `bool` | `false` | alternating row backgrounds |
| `Condensed` | `bool` | `false` | tighter cell padding + smaller icons/text |
| `SortOrder` | `SortOrder` | `Ascending` | initial sort direction |
| `MaxCellCharsToDisplay` | `int` | `0` | truncate long cell text with `…` (`0` = no limit; full value still in the cell tooltip) |
| `TableRowContextMenuDisplayStyle` | enum | `DropDown` | row actions as `Icons` inline or a `DropDown` ⋯ menu |
| `FilterDebounceMilliseconds` | `int` | `400` | pause after typing before a column filter applies |
| `NoDataAvailableTitle` | `string` | `"No data available to display"` | empty-state text |
| `FilterPlaceholderText` | `string` | `"Search..."` | filter input placeholder |
| `ContentMenuTitle` | `string` | `"Actions"` | tooltip on the ⋯ context-menu toggle |
| `RowsPerPageText` / `SelectNumberOfRowsToDisplayText` | `string` | see default | footer labels |
| `PaginationHorizontalAlignment` | enum | `Right` | footer alignment |

Page-size choices are fixed at **10 / 25 / 50 / 100** (default 10); any other
`NumberOfItemsPerPage` is coerced to 10. A "Set Visible Columns" dialog (footer)
lets the end user toggle column visibility at runtime.

## Defining Columns — the model & its attributes

Columns come from **public properties of `TModel` marked with an attribute**.
Unmarked properties are ignored. Put these in
`Maviray.Blazor.Components.Pro.Core.Attributes.Grid`.

| Attribute | On | Does |
|---|---|---|
| `[GridDataProperty(sequence)]` | a value property | makes it a column; `sequence` sets left-to-right order (ties → declaration order) |
| `[GridDataProperty(sequence, MaviGridColumnCellBehavior.Navigational)]` | " | same, but cells render bold/clickable and emit `OnCellClick` with `CellBehavior = Navigational` (use for "open detail") |
| `[Display(Name = "…")]` | a column property | header text (falls back to the property name) |
| `[GridBooleanDisplayValues("Yes","No")]` | a `bool` property | positive/negative display text (default `True`/`False`); also used to match typed filter text |
| `[GridDateTimeFormat("yyyy-MM-dd")]` | a date property | custom format for `DateTime`/`DateTimeOffset` cells (default `yyyy-MM-dd HH:mm:ss`) |
| `[GridAdvancedDisplay]` | a column property | render cells as **badges**; color per cell comes from a `cellThemeResolvers` entry (below) |
| `[GridNavProperty(sequence)]` | a property whose type is another `IGridCollectionModel` | flatten a related model's `[GridNestedProperty]` columns in; auto eager-loaded by property name |
| `[GridNestedProperty("Header")]` | a property inside a nav model | that property becomes a column, keyed `NavProp.Prop` |

**Column key** = the model property name (nested: `NavProp.Prop`). Keys are what
`OrderByColumnKey`, filters, cell-click, and `cellThemeResolvers` reference.

**Value formatting** is automatic: bools → your positive/negative labels; enums →
`[Display]` name of the member; dates → the format above; numbers →
culture-invariant. `ConstructFrom` values are read after formatting rules apply.

### Sortable / filterable resolution (important)

A column is sortable **and** filterable only if its key resolves to a real path
on `TDataItem` — either an **identically-named member**, or an explicit
**`GridPropertyPathMap`** entry. If neither exists (a genuinely computed column
like `Total = Qty * Price`), the column still displays but gets **no sort arrow
and no filter box** — it is not an error. This is the mechanism, not a bug: to
make a related/computed column sortable, give it a path map entry pointing at the
entity dot-path it derives from.

### Populating computed & related columns — `ConstructFrom`

The factory first Mapster-maps same-named entity properties onto the model, then
calls `ConstructFrom(entity)`. Use it for anything not a direct 1:1 copy:

```csharp
public void ConstructFrom(Order entity)
{
    CustomerName = entity.Customer?.CompanyName;                 // related
    CountryName  = entity.Customer?.Region?.Country?.CountryName; // deep related
    Total        = entity.Lines.Sum(l => l.Qty * l.UnitPrice);   // computed
}
```

For related data to be loaded, eager-load it via `includeConfigurator` (below) or
`[GridNavProperty]`.

## The Data Factory — `BuildDataCollection`

`IGenericGridDataFactory<TDataItem, TModel, TKey>` is injected and called inside
`FetchData`. One required arg plus four optional hooks:

```csharp
Task<MaviGridCollection<TKey>?> BuildDataCollection(
    MaviGridDataQueryParameters queryParameters,
    Expression<Func<TDataItem, bool>>? additionalFilter = null,
    IReadOnlyDictionary<string, Func<MaviGridCell<TKey>, object?, ThemeColorScheme>>? cellThemeResolvers = null,
    GridIncludeConfigurator<TDataItem>? includeConfigurator = null,
    GridPropertyPathMap<TDataItem, TModel>? propertyPathMap = null);
```

| Arg | Use |
|---|---|
| `additionalFilter` | a per-call predicate AND-combined with the grid's filters (e.g. `x => x.OwnerId == userId`); limits both the count and the page |
| `cellThemeResolvers` | per-column (keyed by column key) delegates that return a `ThemeColorScheme` for each badge cell of a `[GridAdvancedDisplay]` column; receives the cell and the **raw** value |
| `includeConfigurator` | shape the root query before paging — EF `Include(...).ThenInclude(...)` — so the graph is loaded for `ConstructFrom` |
| `propertyPathMap` | map a model property → entity dot-path (with optional `nullDefault`) so related/coalesced columns become sortable/filterable |

### Related columns done right (flat model + path map + includes)

Preferred pattern for related data: flat `[GridDataProperty]` columns filled in
`ConstructFrom`, made sortable/filterable with a path map, graph loaded with an
include configurator.

```csharp
private static readonly GridPropertyPathMap<Order, OrderGridModel> PathMap = new GridPropertyPathMap<Order, OrderGridModel>()
    .Map(m => m.CustomerName, "Customer.CompanyName")
    .Map(m => m.CountryName,  "Customer.Region.Country.CountryName")
    .Map(m => m.RegionRating, "Customer.Region.Rating", nullDefault: RatingEnum.Low); // coalesce to match ConstructFrom's ?? 

private static IQueryable<Order> Include(IQueryable<Order> q) =>
    q.Include(o => o.Customer).ThenInclude(c => c!.Region).ThenInclude(r => r!.Country);

private Task<MaviGridCollection<int>?> FetchGridData(MaviGridDataQueryParameters p)
    => DataFactory!.BuildDataCollection(p, includeConfigurator: Include, propertyPathMap: PathMap);
```

`nullDefault` keeps DB-side sort/filter consistent with a `?? default` applied in
`ConstructFrom`. Alternatively, the `[GridNavProperty]`/`[GridNestedProperty]`
nested-model approach flattens one related model automatically (auto-included by
property name) — simpler for a single-hop relation, less flexible than the path
map for deep graphs or computed values.

### Badge / status columns

Mark the property `[GridAdvancedDisplay]`, then pass a resolver keyed by that
column's key:

```csharp
var resolvers = new Dictionary<string, Func<MaviGridCell<int>, object?, ThemeColorScheme>>
{
    ["Status"] = (cell, value) => value?.ToString() switch
    {
        "Paid"    => ThemeColorScheme.Success,
        "Overdue" => ThemeColorScheme.Alert,
        _          => ThemeColorScheme.Default,
    },
};
return await DataFactory.BuildDataCollection(p, cellThemeResolvers: resolvers);
```

## Sorting, Paging, Filtering behavior

- **`MaviGridDataQueryParameters`** carries the request: `SortOrder`,
  `NumberOfItemsPerPage` (10/25/50/100), `PageNumber`, `OrderByColumnKey`,
  `Filters`. Helpers: `AddFilter(key, value, criteria)`, `RemoveFilter(key)`,
  `ClearFilters()`. The grid mutates this object and re-calls `FetchData`; set a
  default `OrderByColumnKey` when it's blank (the factory defaults to `"Id"`).
- **Sorting**: clicking a sortable header sets `OrderByColumnKey` and toggles
  direction. Initial direction = `Parameters.SortOrder`.
- **Filtering** (`EnableFilters = true`): a debounced text box per *filterable*
  column. The grid picks criteria by the column's CLR category — **string →
  `Contains`**, numeric/date/guid/timespan → `Equals` (text must parse or the
  filter is dropped), **bool → matches your `Yes/No` labels**, **enum → matches
  `[Display]` names**. So users filter bools/enums by the text they see.

## Context Menus, Cell Clicks, Selection

**Row actions** — return them from the model's `GetContextActions()`:

```csharp
public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() =>
[
    new MaviRowContextMenuItem<int> { Key = Id, Title = "Edit",   Icon = "lni lni-pencil-1",
        ThemeColorScheme = ThemeColorScheme.Primary, ElementSize = ElementSize.Small, ElementVariant = ElementVariant.Outlined },
    new MaviRowContextMenuItem<int> { Key = Id, Title = "Delete", Icon = "lni lni-trash-3",
        ThemeColorScheme = ThemeColorScheme.Alert,   ElementSize = ElementSize.Small, ElementVariant = ElementVariant.Outlined },
];
```

Handle clicks with `OnRowContextMenuClick` (the clicked `IMaviRowContextMenuItem<TKey>`;
`Key` is the row key). Render style is `Parameters.TableRowContextMenuDisplayStyle`
(`Icons` | `DropDown`).

**Header menu** — grid-wide actions (Add, Refresh, Delete Selected, …):

```razor
<MaviGrid ... ConstructMainContextMenu="BuildMenu" OnMainContextMenuButtonClick="OnMenuClick" />
```
```csharp
private Task<IEnumerable<IMaviContextMenuItem>> BuildMenu() => Task.FromResult<IEnumerable<IMaviContextMenuItem>>(
[
    new MaviContextMenuItem { Key = "add",     Title = "Add" },
    new MaviContextMenuItem { Key = "refresh", Title = "Refresh" },
]);

private async Task OnMenuClick(IMaviContextMenuItem item)
{
    if (item.Key == "refresh" && _grid is not null) await _grid.Refresh();
    // "add" → open your dialog; combine with GetSelectedRows() for bulk actions
}
```

**Cell click** — `OnCellClick` gives `CellClickParameters<TKey>` (`RowKey`,
`ColumnKey`, `CellBehavior`); check `CellBehavior == Navigational` to treat a
bold/clickable cell as "open this row".

## Global / Scoped Filters (cross-cutting)

To constrain **every** grid (soft-delete, tenant, permission scope) without
per-page wiring, implement `IGridGlobalFilterProvider` and register it. Its filter
is AND-combined with per-call and column filters, applied to count and page:

```csharp
public class TenantGridFilter : IGridGlobalFilterProvider
{
    private readonly ITenantContext _tenant;
    public TenantGridFilter(ITenantContext tenant) => _tenant = tenant;

    public Expression<Func<TDataItem, bool>>? GetFilter<TDataItem>() where TDataItem : class
    {
        if (!typeof(ITenantScoped).IsAssignableFrom(typeof(TDataItem))) return null; // only constrain opted-in entities
        var id = _tenant.Id;
        return Expression.Lambda<Func<TDataItem, bool>>( /* x => ((ITenantScoped)x).TenantId == id */ ...);
    }
}
```

Register: `AddGridGlobalFilter<TenantGridFilter>()` (type, scoped, can take
deps) or `AddGridGlobalFilter(instance)` (singleton, stateless). Multiple
providers are all AND-combined.

## Data Source

The factory reads through `IGridDataSource<TDataItem>` (`Count` + paged `Get`).

**EF Core (typical):** subclass the provided source, binding it to your
`DbContext`, and register the open generic:

```csharp
public class AppGridDataSource<TDataItem> : EntityFrameworkGridDataSource<TDataItem, AppDbContext>
    where TDataItem : class
{
    public AppGridDataSource(IDbContextFactory<AppDbContext> f, ILogger<AppGridDataSource<TDataItem>> l) : base(f, l) { }
}
```

It uses `IDbContextFactory<TDbContext>`, queries `AsNoTracking`, orders
dynamically by the resolved path, and applies `includePaths` + your
`includeConfigurator`. For a non-EF backing store, implement `IGridDataSource<T>`
directly.

## Dependency Injection

```csharp
builder.Services.AddMaviComponents();                    // Material/Core services (required)
builder.Services.RegisterGridDataFactory();              // IGenericGridDataFactory<,,>
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(cs));  // EF source needs a FACTORY
builder.Services.AddTransient(typeof(IGridDataSource<>), typeof(AppGridDataSource<>)); // open generic
builder.Services.AddGridGlobalFilter<TenantGridFilter>();  // optional, repeatable
```

## Host Setup (assets & render mode)

In your host page (`App.razor` / `_Host.cshtml`):

```html
<!-- CSS -->
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Pro.Grid.UI/css/maviray.grid.min.css" />
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Material/css/maviray.material.min.css" />
<link rel="stylesheet" href="https://cdn.lineicons.com/5.0/lineicons.css" />  <!-- lni icons (menus, pager) -->
<!-- JS: powers outside-click closing of the menus -->
<script src="_content/Maviray.Blazor.Components.Material/js/maviJsInterop.js"></script>
```

The grid is **interactive** — render it under an interactive render mode
(`InteractiveServer` or `InteractiveWebAssembly`); a static-rendered grid won't
page, sort, or filter. If you author your own Tailwind utilities around the grid,
see the `material-css-styling` skill.

## Enums (reference)

- `MaviGridColumnCellBehavior`: `Default`, `Navigational`
- `TableRowContextMenuDisplayStyle`: `Icons`, `DropDown`
- `SortOrder`: `Ascending`, `Descending`
- `ThemeColorScheme` (badges, row actions): `Default`, `Primary`, `Secondary`, `Success`, `Alert`, `Warning`, `Info`, `Dark`, `Light`
- `MaviGridFilterCriteria`: `None`, `Equals`, `NotEquals`, `Contains`, `Excludes`, `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual`, `IsTrue`, `IsFalse`, `Before`, `After`
- `MaviGridPropertyType`: `String`, `Char`, `Bool`, `DateTime`, `TimeSpan`, `Int`, `Decimal`, `Guid`, `Enum`, `Undefined` + `Nav*` variants (all integral types collapse to `Int`, all real types to `Decimal`, all point-in-time types to `DateTime`)

## Gotchas & Do / Don't

| Do | Don't |
|---|---|
| Give every visible column a `[GridDataProperty(seq)]` | expect unmarked properties to appear |
| Return a page from `FetchData` via `BuildDataCollection` | fetch/sort/page the whole table yourself — it's server-paged |
| Add a `GridPropertyPathMap` entry to make a related/computed column sortable | wonder why a computed column has no sort arrow / filter box — that's by design until mapped |
| Eager-load related data (`includeConfigurator` or `[GridNavProperty]`) before reading it in `ConstructFrom` | read `entity.Related.X` without loading `Related` (it'll be null) |
| Use a `DbContextFactory` + the EF source subclass bound to your context | register a scoped `DbContext` and expect the factory source to work |
| Call `_grid.Refresh()` after edits/deletes | mutate the page list in place and expect the grid to notice |
| Filter bools/enums by their **display** text (`Yes`, a `[Display]` name) | filter them by `true`/`false` or raw enum member names |
| Put `maviJsInterop.js` + the grid/material CSS in the host and use an interactive render mode | ship a static-rendered grid and expect interactivity |

## Pointers

- **Installed version:** the app's `.csproj`
  `<PackageReference Include="Maviray.Blazor.Components.Pro.Grid.UI" Version="…" />`.
- **Ground truth if a local `blazor-ui-kit-pro` checkout is an added directory:**
  `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor`
  (component + render), `.../Components/MaviGridParameters.cs`,
  `src/Maviray.Blazor.Components.Pro.Core/Attributes/Grid/**` (attributes),
  `.../Services/GenericGridDataFactory.cs` (the pipeline),
  `.../Interfaces/Grid/**`, `.../Models/Grid/**`, `.../Enums/Grid/**`,
  `.../Extensions/DependencyInjectionExtensions.cs`, and the living examples in
  `src/Maviray.Blazor.Components.Pro.Grid.Sample/Components/Pages/**`
  (`Home.razor` = menus/badges, `PropertyMapping.razor` = path map/includes,
  `Scoped.razor` = global filter). ⚠️ A checkout may differ in version from the
  installed package — verify against the `<PackageReference>`.
- **Otherwise:** rely on the tables above and IntelliSense over the referenced assemblies.

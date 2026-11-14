---
name: mavi-grid
description: >-
  Use when adding, configuring, or troubleshooting a data grid or table in a
  .NET/Blazor app that consumes the Maviray grid packages (MaviGrid<TKey>,
  IGridDataFactory, Maviray.Blazor.Components.Pro.Grid.*): server-side paging,
  sorting and filtering over large tables, grid columns from a model class, row
  and header menus, selection, row or column access control on grids, grid audit
  logging, slow or double-fetching grids, feeding a grid from your own service or
  DTOs, or errors such as an unresolved IGridDataFactory or IGridDataSource.
  Applies even when the request just says "add a table" or "show these records
  in a grid". Self-contained: the library ships as NuGet packages.
---

# MaviGrid (consumer)

## Overview

`MaviGrid<TKey>` is a server-paged Blazor grid. It never holds the whole table: every page, sort and filter change
calls your `FetchData(query, cancellationToken)` for exactly the rows on screen. You don't write columns or `<tr>`s:
**columns are attributes on a grid model class**, and with EF Core **`IGridDataFactory` does the fetching**.

```
MaviGrid ──(MaviGridQuery, ct)──► FetchData ──► IGridDataFactory.BuildPageAsync(query, options, ct)
  1. access policies     deny → denied page, no database call; allow → row filters + hidden columns
  2. one filter          column filters ∧ options.Filter ∧ global filters ∧ policy row filters
  3. count (skipped when the grid already knows the total), then load only this page (seek or skip)
  4. entity → model (same-named members copied, then model.ConstructFrom), cells formatted
  5. fetch observers (audit)
◄── MaviGridCollection<TKey>
```

The library's source isn't in a consuming app: use this skill, its reference files and IntelliSense.

## Setup

| Dependency | Where | Why |
|---|---|---|
| `Maviray.Blazor.Components.Pro.Grid.UI` | NuGet | the component; brings the grid core and the Material components |
| `Maviray.Blazor.Components.Pro.Grid.EntityFramework` | NuGet | EF Core data source (skip if your own service loads pages) |
| `AddMaviComponents()` | Program.cs | options every Mavi component injects |
| `AddDbContextFactory<TDbContext>()` | Program.cs | a short-lived context per query; grids fetch concurrently |
| `AddMaviGrid().AddEntityFrameworkDataSource<TDbContext>()` | Program.cs | registers `IGridDataFactory<TDataItem, TModel, TKey>` and how rows are read |
| grid + Material CSS, LineIcons, `maviJsInterop.js` | App.razor `<head>` | styles, theme, icons, menus closing on outside click |
| interactive render mode | App.razor or page | clicks and typing work only when interactive |

```csharp
// Program.cs
using Maviray.Blazor.Components.Core.Extensions;                    // AddMaviComponents
using Maviray.Blazor.Components.Pro.Core.Extensions;                 // AddMaviGrid
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions; // AddEntityFrameworkDataSource

builder.Services.AddMaviComponents();
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(connectionString)); // a FACTORY
builder.Services.AddMaviGrid()            // options: RequireExplicitAllow, MaxPageSize, ReuseTotalRows
    .AddEntityFrameworkDataSource<AppDbContext>();
```

```html
<!-- App.razor <head> -->
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Pro.Grid.UI/css/maviray.grid.min.css" />
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Material/css/maviray.material.min.css" />
<link rel="stylesheet" href="https://cdn.lineicons.com/5.0/lineicons.css" />
<script src="_content/Maviray.Blazor.Components.Material/js/maviJsInterop.js"></script>
<!-- and <Routes @rendermode="InteractiveServer" /> (or WebAssembly / Auto) -->
```

The pager's next-page icon (`lni-chevron-right`) is in LineIcons Pro only: with the free 5.0 stylesheet the button
has no arrow. Load the app's LineIcons Pro stylesheet if it has one.

`_Imports.razor`: `Maviray.Blazor.Components.Pro.Grid.UI.Components` (MaviGrid, MaviGridParameters),
`…Pro.Core.Models.Grid` (MaviGridQuery, MaviGridCollection, GridFetchOptions), `…Pro.Core.Interfaces.Grid`
(IGridDataFactory). Full namespace table: `references/api.md`.

## Minimal grid

```csharp
public class OrderGridModel : IGridCollectionModel<Order, int>      // entity, row key type
{
    [GridDataProperty(1)] public int Id { get; set; }                // same name as an entity member:
    [GridDataProperty(2)] public string Number { get; set; } = "";   // copied, sortable, filterable
    [Display(Name = "Total")] [GridDataProperty(3)] public decimal Total { get; set; }
    public int Key => Id;
    public void ConstructFrom(Order order) { }                        // computed columns go here
    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}
```

```razor
@page "/orders"
@inject IGridDataFactory<Order, OrderGridModel, int> GridData

<MaviGrid TKey="int" FetchData="FetchPageAsync" />

@code {
    private Task<MaviGridCollection<int>> FetchPageAsync(MaviGridQuery query, CancellationToken cancellationToken) =>
        GridData.BuildPageAsync(query, cancellationToken: cancellationToken);
}
```

## I need to… → where

| Need | Use | Recipe (`references/recipes.md`) |
|---|---|---|
| initial sort, page size, filter row | `MaviGridParameters` | Sorting, paging, filtering |
| Edit / Delete per row | `GetContextActions()` + `OnRowContextMenuClick` | Row actions |
| Export / Add / Refresh for the grid | `ConstructMainContextMenu` + `OnMainContextMenuButtonClick` | Header menu |
| open an item from a cell | `Navigational` column + `OnCellClick` | Cell clicks |
| checkboxes, hide columns, reload | `DisplaySelectColumn`, `@ref` + `SelectedKeys`, `SetColumnVisible`, `RefreshAsync` | Selection |
| striped, condensed, translated texts | `MaviGridParameters` | Look and texts |
| coloured badges, date / bool / enum text | attributes + `GridFetchOptions.CellThemes` | Badges and formatting |
| columns from related entities, sortable computed columns | `Include`, `GridPropertyPathMap`, `[GridNavProperty]` | Related data |
| only some rows (this grid / every grid) | `GridFetchOptions.Filter` / `IGridGlobalFilterProvider` | Narrowing rows |
| custom empty / error / denied / loading UI | `NoDataContent`, `FetchErrorContent`, `AccessDeniedContent`, `PrerenderContent` | States |
| who may see which rows and columns, audit | `GridAccessPolicy<T>`, `WithHiddenColumns`, `IGridFetchObserver` | Access and audit |
| large tables, wide entities, one query per view | count reuse, keyset, `Projection`, `PrerenderedPageKey` | Fast paging |
| pages come from my own service / DTOs | `GridPageBuilder`, `GridKeyset` | Your own service |
| a non-EF store | `IGridDataSource<T>` (+ `IGridKeysetDataSource<T>`) | Custom data source |

Every parameter, method, option, attribute and type, with defaults: `references/api.md`.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `Cannot provide a value for property 'ComponentOptions'` | add `AddMaviComponents()` |
| `Unable to resolve service for type 'IGridDataFactory…'` | add `AddMaviGrid()` |
| `Unable to resolve service for type 'IGridDataSource…'` | add `.AddEntityFrameworkDataSource<TDbContext>()` (or your own source) |
| `A second operation was started on this context` | use `AddDbContextFactory`, not only `AddDbContext` |
| `The type arguments … TKey … cannot be inferred` | set `TKey="int"` (or your key type) on `<MaviGrid>` |
| clicks, sorting, typing do nothing | the page isn't in an interactive render mode |
| unstyled grid, missing icons, menus never close | the stylesheets, LineIcons or `maviJsInterop.js` are missing |
| a column has no sort button or filter box | it has no data item path: name it like the entity member, or map it (`GridPropertyPathMap`) |
| related values empty in `ConstructFrom` | load them: `Include` or `[GridNavProperty]`; with a `Projection`, `.Load(x => x.Member)` |
| every view queried (and audited) twice | set `PrerenderedPageKey` (or `FetchWhilePrerendering = false`) |
| totals stale after your own edits | call `RefreshAsync()`: page clicks reuse the known total |
| grid shows "Data could not be loaded" | `FetchData` threw; handle `OnFetchError`, check the log |
| `OptionsValidationException` at startup | `MaxPageSize` is below 1 |
| long text cut off while the actions column is wide | mark that column `[GridStretch]` (or set `StretchColumns`): the actions column fills only when no column stretches |
| grid doesn't fill its card, or should shrink | `FillWidth` (default `true`), not a `block` / `inline-block` class: those resolve by stylesheet order |
| next-page button has no arrow | free LineIcons 5.0 has no `lni-chevron-right`; load LineIcons Pro |
| `[GridStretch] weight must be at least 1` / `ArgumentOutOfRangeException` on `StretchColumns` | use weights ≥ 1 |

## Rules

| Do | Don't |
|---|---|
| create `MaviGridParameters` and `GridFetchOptions` once (field / static) | build them, maps or theme dictionaries inside `FetchData` |
| pass `FetchData`'s token to `BuildPageAsync` | drop it: superseded fetches then run to the end |
| mark every column `[GridDataProperty]` | expect unmarked properties to show |
| return `NotApplicable()` from a policy for grids it doesn't govern | return `Deny` by default: it blocks every grid |
| check permission again where you handle a row action or act on `SelectedKeys` | trust that a shown action is allowed |
| use an access policy for permissions | use a global filter for permissions |

## Pointers

- A live, runnable version of every recipe: the `/docs` page of the `Maviray.Blazor.Components.Pro.Grid.Sample` project
  in the `blazor-ui-kit-pro` repository.
- Installed version: the app's `<PackageReference Include="Maviray.Blazor.Components.Pro.Grid.UI" … />`.
- Styling around the grid: the `material-css-styling` skill. Other Mavi components (buttons, dialogs, inputs): the
  `maviray-blazor-components` skill.

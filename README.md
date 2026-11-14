# Maviray Blazor UI Kit Pro

Blazor components for apps that work with large tables, built on the
[Maviray Blazor UI Kit](https://github.com/Maviray/blazor-ui-kit).

The kit has one component today: `MaviGrid`, a server-paged data grid. Every page, sort or filter change loads only the
rows being shown, with the sort and filters applied in the database. You describe the columns once, with attributes on a
model class. Access policies decide before every fetch who may read which rows, and fetch observers record what each
user was shown.

- **.NET 10**, for Blazor apps with interactive rendering. EF Core works out of the box, and any other store plugs in
  through one interface
- **Fast on large tables**: paging, sorting and filtering run in the database, next and previous pages seek from the
  last row instead of skipping rows, and the total is counted once per set of filters
- **Columns from a model**: attributes set each column's header, format, badge colour and width, and sorting and
  filtering follow from the model
- **Access control and audit**: allow, deny or narrow every fetch per user, and record which rows each user saw
- **Matches the UI Kit**: built from its Material components and theme. The packages ship prebuilt CSS, so using the
  grid needs no Node.js and no Tailwind build

## Packages

| Package | Contents |
|---|---|
| `Maviray.Blazor.Components.Pro.Grid.UI` | The `MaviGrid` component and its stylesheet. **Install this one**, plus a data source. |
| `Maviray.Blazor.Components.Pro.Grid.EntityFramework` | The EF Core data source |
| `Maviray.Blazor.Components.Pro.Core` | Grid models and attributes, the data factory, access policies, fetch observers and service registration. Installed with either package above. |

The packages also install the UI Kit's `Maviray.Blazor.Components.Material` and `Maviray.Blazor.Components.Core`.

## Components

| Component | What it is |
|---|---|
| `MaviGrid<TKey>` | Server-paged data grid: sortable headers, a filter row, a pager with page-size choices, row checkboxes whose selection survives paging, a column chooser, header and row action menus, clickable cells, badge cells, stretching columns, and built-in empty, loading, access-denied and error states |

Behind the grid are services you register once and extend:

| Type | What it does |
|---|---|
| `IGridDataFactory<TDataItem, TModel, TKey>` | Builds one page: checks access, combines the filters, counts, loads the rows, maps them to your model and formats the cells |
| `GridPageBuilder<TDataItem, TModel, TKey>` | Builds the same page from rows your own service loaded, for apps with a service layer of their own |
| `AddEntityFrameworkDataSource<TDbContext>()` | The EF Core data source, for every entity type in the context |
| `IGridDataSource<T>`, `IGridKeysetDataSource<T>` | Plug in another store, with or without keyset paging |
| `IGridAccessPolicy`, `GridAccessPolicy<T>` | Allow, deny or narrow a fetch before it runs |
| `IGridFetchObserver` | Told about every page loaded and every denied fetch, for audit |
| `IGridGlobalFilterProvider` | One constraint for every grid, such as "not soft-deleted" |
| `[GridDataProperty]` and the other [column attributes](#columns) | Turn a model's properties into columns |

Forms, buttons, dialogs, menus and the other general-purpose components are in the UI Kit. See its
[component list](https://github.com/Maviray/blazor-ui-kit#components).

## Installation

The packages are published to GitHub Packages, which needs a GitHub token even to download them. If your app already
uses the UI Kit, steps 1 to 3 are done.

1. Create a GitHub personal access token (classic) with the `read:packages` scope.
2. Store it once per machine. Run this **outside any repository**: inside one, the command writes the token into that
   repository's `nuget.config`.

   ```bash
   dotnet nuget add source "https://nuget.pkg.github.com/Maviray/index.json" --name maviray --username YOUR_GITHUB_USERNAME --password YOUR_TOKEN
   ```

   On macOS and Linux, add `--store-password-in-clear-text`; Windows encrypts the password.
3. In your app's repository, add a `nuget.config` that takes Maviray packages from that feed and everything else from
   nuget.org. Restores then work the same on every machine, and central package management doesn't warn about several
   feeds (NU1507). The `maviray` key must match the name used in step 2, which is how NuGet finds the token.

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <clear />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
       <add key="maviray" value="https://nuget.pkg.github.com/Maviray/index.json" />
     </packageSources>
     <packageSourceMapping>
       <packageSource key="nuget.org">
         <package pattern="*" />
       </packageSource>
       <packageSource key="maviray">
         <package pattern="Maviray.*" />
       </packageSource>
     </packageSourceMapping>
   </configuration>
   ```

4. Add the packages:

   ```bash
   dotnet add package Maviray.Blazor.Components.Pro.Grid.UI
   dotnet add package Maviray.Blazor.Components.Pro.Grid.EntityFramework
   ```

In GitHub Actions, add the source with `${{ secrets.GITHUB_TOKEN }}` as the password and give the job
`permissions: packages: read`.

## Quick start

**1. Register the services** in `Program.cs`:

```csharp
using Maviray.Blazor.Components.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions;

builder.Services.AddMaviComponents();
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(connectionString)); // or AddPooledDbContextFactory

builder.Services.AddMaviGrid()
    .AddEntityFrameworkDataSource<AppDbContext>();
```

**2. Add the assets** to the `<head>` of `App.razor`:

```html
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Pro.Grid.UI/css/maviray.grid.min.css" />
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Material/css/maviray.material.min.css" />
<link rel="stylesheet" href="https://cdn.lineicons.com/5.0/lineicons.css" />
<script src="_content/Maviray.Blazor.Components.Material/js/maviJsInterop.js"></script>
```

The theme's fonts aren't bundled. Load Lato, Montserrat, Oswald and Roboto Serif, for example from Google Fonts.

The pager's next-page icon, `lni-chevron-right`, is in LineIcons Pro. The free LineIcons 5.0 stylesheet above doesn't
have it, so there the button shows no arrow (its tooltip still works). Load a LineIcons Pro stylesheet to see it.

**3. Describe the columns on a grid model**

```csharp
public class OrderGridModel : IGridCollectionModel<Order, int>
{
    [GridDataProperty(1)] public int Id { get; set; }
    [Display(Name = "Customer")]
    [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)] public string? CustomerName { get; set; }
    [GridDataProperty(3)] [GridDateTimeFormat("yyyy-MM-dd")] public DateTime OrderedOn { get; set; }
    [GridDataProperty(4)] [GridBooleanDisplayValues("Yes", "No")] public bool Shipped { get; set; }

    public int Key => Id;

    // Properties with the same name as the entity's are mapped automatically; set the rest here.
    public void ConstructFrom(Order order) => CustomerName = order.Customer?.Name;

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() =>
        [new MaviRowContextMenuItem<int> { Key = Id, Title = "Open", Icon = "lni lni-eye" }];
}
```

**4. Show the grid** in a component with an interactive render mode:

```razor
<MaviGrid TKey="int" FetchData="FetchPageAsync" Parameters="_parameters" OnCellClick="OpenOrder" />

@code {
    private static readonly GridFetchOptions<Order, OrderGridModel> Options = new()
    {
        Include = q => q.Include(o => o.Customer),
        PropertyPathMap = new GridPropertyPathMap<Order, OrderGridModel>().Map(m => m.CustomerName, "Customer.Name")
    };

    private readonly MaviGridParameters _parameters = new() { EnableFilters = true, SortColumnKey = "Id" };

    [Inject] private IGridDataFactory<Order, OrderGridModel, int> Grid { get; set; } = default!;

    private Task<MaviGridCollection<int>> FetchPageAsync(MaviGridQuery query, CancellationToken cancellationToken) =>
        Grid.BuildPageAsync(query, Options, cancellationToken);
}
```

Pass the cancellation token through. The grid cancels it when a newer fetch starts or the grid is disposed.

## How a page is fetched

```
MaviGrid ──(MaviGridQuery, CancellationToken)──► your FetchData
  IGridDataFactory.BuildPageAsync
    1. access policies             deny → a column-only "access denied" page, no database call
    2. one combined filter         column filters AND grid filter AND global filters AND policy row filters
    3. IGridDataSource.CountAsync  skipped when the grid already has the total for these filters
    4. load the page               skipped when the count is 0; ORDER BY … key, then seek from a cursor or OFFSET
    5. map rows to the model, format cells with cached getters
    6. fetch observers             the keys of the rows loaded
  ◄── MaviGridCollection<TKey>
```

What keeps it fast on large tables:

- **Only the page is loaded.** The EF source runs a count and one page query, without change tracking, on a
  short-lived context. Set `GridFetchOptions.Projection` to select only the shown columns instead of whole entities.
- **Next and previous seek.** Moving one page on, the grid sends a cursor and the EF source seeks from the last row
  seen (`WHERE … > …`) instead of skipping rows (`OFFSET`), so page 5,000 costs the same as page 2. Jumping to a page
  number skips as usual. Seeking needs a sort on a column of the entity itself that isn't null (or has a null default).
- **One count per filter.** When only the page, the page size or the sort changes, the grid sends the total it has and
  the factory skips the count. It counts again when the filters change, on `RefreshAsync()`, and when a page doesn't
  hold the number of rows the total predicts.
- **Stable paging.** The order always ends with the primary key, so rows with equal sort values never repeat or go
  missing between pages.
- **One query plan per grid.** Filter values are sent as SQL parameters, not literals.
- **Cheap rows.** Column metadata, compiled property getters and the entity-to-model mapping are built once per model
  type; column lists are cached per grid. Cells are value types.
- **Cheap rendering.** The grid re-renders only when what it shows changes. Typing in a filter or opening a menu doesn't
  redraw the rows, and each row redraws only when its data, selection or menu changes.
- **Nothing wasted.** A newer fetch cancels the one in flight. The grid can reuse the page it fetched while
  prerendering (`PrerenderedPageKey`) or skip that fetch (`FetchWhilePrerendering = false`).

## Columns

| Attribute | On | Effect |
|---|---|---|
| `[GridDataProperty(sequence, behavior)]` | model property | a column; `Navigational` cells are bold and meant to be clicked |
| `[Display(Name = …)]` | model property, enum member | header text; enum cell and filter text |
| `[GridNavProperty(sequence)]` | property holding a related model | eager-loads the navigation of the same name; its `[GridNestedProperty]` properties become columns keyed `Navigation.Property` |
| `[GridNestedProperty(title)]` | property of the related model | a nested column |
| `[GridBooleanDisplayValues(trueText, falseText)]` | bool property | cell and filter text |
| `[GridDateTimeFormat(format)]` | date/time property | cell format (invariant culture) |
| `[GridAdvancedDisplay]` | any column | renders as a badge coloured by `GridFetchOptions.CellThemes` |
| `[GridStretch(weight = 1)]` | any column, or a navigation property (applies to its nested columns) | the column shares the free width with other stretching columns, by weight |

**Column widths.** The grid fills its parent's width. Columns keep their width (up to 200px, more if the content needs
it) unless they stretch. With no stretching column visible, the actions column takes the free width, so the ⋯ menu
sits at the right edge. `MaviGridParameters.StretchColumns` chooses the stretching columns for one grid instead. The
free width is what is left once every other column reaches 200px, so in a narrow grid a stretching column gets only the
room its content needs.

**Sorting and filtering.** A column is sortable and filterable when it resolves to a data item path: either a member
with the same name on the entity, or an entry in the `GridPropertyPathMap`. The factory accepts only these column keys;
sorts and filters on anything else are ignored, so no request can sort or filter by a property the grid doesn't show.
A computed column with no single path stays display-only.

## `GridFetchOptions<TDataItem, TModel>`

The settings that stay the same from page to page. Create them once per grid, for example as a static field, so the
factory reuses the columns it built for them.

| Property | Use |
|---|---|
| `Filter` | a row constraint for this grid, AND-combined with everything else |
| `Include` | `Include(…).ThenInclude(…)` so `ConstructFrom` sees the related data |
| `PropertyPathMap` | data item paths for computed columns, optionally with a null default (`?? false`) |
| `Projection` | select only the shown columns instead of whole entities with their includes; list any other member `ConstructFrom` reads with `.Load(x => x.Notes)` |
| `CellThemes` | badge colour per graphic column: `value => ThemeColorScheme` |
| `AccessTag` | a tag passed to access policies and observers; no meaning to the grid |

## Data sources

`AddEntityFrameworkDataSource<TDbContext>()` serves every entity type. For a context per entity, or another store,
implement `IGridDataSource<TDataItem>` and register it for that type, or as an open generic with
`.AddDataSource(typeof(MySource<>))`:

- `CountAsync(query, ct)` counts rows matching `query.Filter`;
- `GetPageAsync(query, ct)` applies includes, filter and sort, then `Skip`/`Take`, and returns materialized rows;
- keep it stateless (open and close connections inside each call);
- order deterministically: break ties with a unique key, or paging repeats and skips rows. `ThenByDynamic` helps.

To seek instead of skip, implement `IGridKeysetDataSource<TDataItem>` too. `GridKeyset<T>` implements the ordering,
the cursors and the seek over an `IQueryable<T>`.

When your app loads data through its own service layer (DTOs, its own authorization), skip the factory: call the
service from `FetchData` and turn its result into a page with `GridPageBuilder<TDataItem, TModel, TKey>`, which gives
the same columns, cells and formatting. The sample app's `/docs` page shows it.

EF tips: use `AddPooledDbContextFactory` for high traffic, and configure
`UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` when grids include collection navigations.

## Access policies

An `IGridAccessPolicy` runs before every fetch and returns a decision for the data item type:

| Decision | Effect |
|---|---|
| `NotApplicable()` | the policy has no rule here |
| `Allow(rowFilter?)` | the fetch may run; the row filter (if any) limits the count and the page |
| `Deny(message?)` | no database call; the grid shows its access-denied state with the message |

Policies run in registration order. The first `Deny` wins, and the row filters of every `Allow` are AND-combined. A
policy that throws, or returns no decision, fails the fetch; it never counts as allowed. With
`AddMaviGrid(o => o.RequireExplicitAllow = true)`, a fetch that no policy allows is denied (fail-closed).

```csharp
// One entity type: derive from GridAccessPolicy<TEntity>, which is NotApplicable for every other type.
public sealed class OrderAccessPolicy(AuthenticationStateProvider auth) : GridAccessPolicy<Order>
{
    protected override async ValueTask<GridAccessDecision<Order>> EvaluateAsync(GridAccessRequest request, CancellationToken ct)
    {
        var user = (await auth.GetAuthenticationStateAsync()).User;

        if (!user.IsInRole("Sales"))
        {
            return GridAccessDecision<Order>.Deny("Only the sales team can see orders.");
        }

        var region = user.FindFirst("region")?.Value;
        return GridAccessDecision<Order>.Allow(o => o.Region == region);
    }
}

builder.Services.AddMaviGrid(o => o.RequireExplicitAllow = true)
    .AddEntityFrameworkDataSource<AppDbContext>()
    .AddAccessPolicy<OrderAccessPolicy>();
```

**Cross-cutting rules over an interface** (`ITenantScoped`, `IOwned`, …) implement `IGridAccessPolicy` directly and build
the row filter with `GridFilterExpression.ForInterface`, which rewrites `x => x.TenantId == id` onto the entity's own
property so EF Core can translate it:

```csharp
public async ValueTask<GridAccessDecision<TDataItem>> EvaluateAsync<TDataItem>(GridAccessRequest request, CancellationToken ct)
    where TDataItem : class
{
    if (!typeof(ITenantScoped).IsAssignableFrom(typeof(TDataItem)))
        return GridAccessDecision<TDataItem>.NotApplicable();

    var tenantId = await _tenants.GetCurrentTenantIdAsync(ct);
    Expression<Func<ITenantScoped, bool>> sameTenant = x => x.TenantId == tenantId;
    return GridAccessDecision<TDataItem>.Allow(
        (Expression<Func<TDataItem, bool>>)GridFilterExpression.ForInterface(typeof(TDataItem), sameTenant));
}
```

The interface property must be implemented as a public property; an explicit implementation throws
`InvalidOperationException`.

**Writing policies for Blazor Server.** Policies are scoped, and in Blazor Server a scope is a whole circuit:

- read the user from `AuthenticationStateProvider`, not `IHttpContextAccessor`;
- use `IDbContextFactory` rather than an injected `DbContext`, because two grids on one page fetch at the same time;
- caching a user's grants for the circuit can keep revoked rights alive for hours. Cache with a short expiry (for
  example `HybridCache`) instead;
- keep the deny message free of details you wouldn't show the user.

Use `AccessTag` to give two grids over the same type different rules, for example a user list and an admin list.

## Fetch observers

An `IGridFetchObserver` is told about every fetch:

- `OnRowsLoadedAsync<TDataItem, TKey>` receives the row keys in display order, the page number, the page size and the
  total. It is called for empty pages too.
- `OnAccessDeniedAsync<TDataItem>` is called for every denied fetch.

Observers run inside the fetch, and their exceptions fail it. For best-effort audit, catch inside the observer and hand
the record to a background queue (`Channel<T>` plus a hosted service) instead of writing to the store inline. With
prerendering on, each page view fetches twice. Set `PrerenderedPageKey` (or `FetchWhilePrerendering = false`) so the
audit records one view.

## Global filters

`IGridGlobalFilterProvider` adds a synchronous constraint to every grid of every type, typically "not soft-deleted".
It only narrows rows and never grants access. Register it with `.AddGlobalFilter<T>()`. For tenant isolation across
every query, not only grids, also consider EF Core query filters.

## `MaviGrid<TKey>` reference

| Parameter | Type | Purpose |
|---|---|---|
| `FetchData` (required) | `Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<TKey>>>` | load one page |
| `Parameters` | `MaviGridParameters` | appearance and behaviour |
| `ConstructMainContextMenu` | `Func<Task<IEnumerable<IMaviContextMenuItem>>>` | header menu; built at start and on `RefreshAsync` |
| `OnMainContextMenuButtonClick` | `EventCallback<IMaviContextMenuItem>` | header menu item clicked |
| `OnRowContextMenuClick` | `EventCallback<IMaviRowContextMenuItem<TKey>>` | row action clicked |
| `OnCellClick` | `EventCallback<CellClickParameters<TKey>>` | cell clicked (`RowKey`, `ColumnKey`, `CellBehavior`) |
| `OnFetchError` | `EventCallback<Exception>` | a fetch failed, for example to hand it to an error boundary |
| `PrerenderedPageKey` | `string?` | a key unique among the grids on the page; the interactive grid starts from the page fetched while prerendering, so a page view queries and audits once |
| `NoDataContent`, `AccessDeniedContent`, `FetchErrorContent`, `PrerenderContent` | `RenderFragment` | replace the built-in states |

Exceptions thrown by your own callbacks reach your error boundary. A cancellation caused by the grid itself is never
reported as an error.

| Member | Use |
|---|---|
| `RefreshAsync()` | fetch the current page again (after an edit) |
| `SelectedKeys` / `ClearSelection()` | rows ticked in the select column, kept across pages |
| `SetColumnVisible(key, visible)` / `HiddenColumnKeys` | column visibility, kept across pages (also set by the user in the column dialog) |

**`MaviGridParameters`** (inherits the texts and flags of `TableParameters`):

| Property | Default | Effect |
|---|---|---|
| `SortColumnKey`, `SortOrder` | none, `Ascending` | initial sort |
| `PageSize`, `PageSizeOptions` | `10`, `[10, 25, 50, 100]` | initial page size and the choices offered |
| `EnableFilters` | `false` | filter row; strings match by substring, numbers/dates/ids by equality, bools and enums by their text |
| `FilterDebounceMilliseconds` | `400` | pause after typing before the grid fetches |
| `DisplaySelectColumn` | `false` | row checkboxes |
| `TableRowContextMenuDisplayStyle` | `DropDown` | row actions as icons or a drop-down |
| `Striped`, `Condensed`, `MaxCellCharsToDisplay` | `false`, `false`, `0` | look |
| `NoDataAvailableTitle`, `AccessDeniedTitle`, `FetchErrorTitle` | built-in texts | state texts |
| `FetchWhilePrerendering` | `true` | `false` shows a placeholder while prerendering and fetches once interactively |
| `StretchColumns` | `null` | column key → weight; replaces the model's `[GridStretch]` marks for this grid (assign a new dictionary to change it) |
| `FillWidth` | `true` | fill the parent's width; `false` shrinks the grid to its columns |

**`GridDataFactoryOptions`**, set with `AddMaviGrid(o => …)`:

| Option | Default | Effect |
|---|---|---|
| `MaxPageSize` | `100` | caps the page size the factory loads. The pager shows the size the server applied, even if it isn't one of the `PageSizeOptions` |
| `ReuseTotalRows` | `true` | skip the count when only the page, page size or sort changed. Rows added or removed elsewhere can leave the total stale until the filters change or the grid refreshes |
| `RequireExplicitAllow` | `false` | deny a fetch that no access policy allows |

## Sample app

`src/Maviray.Blazor.Components.Pro.Grid.Sample` runs on an in-memory database, so it needs no database server:

- `/docs`: the documentation page, with every feature as a live grid and its code;
- `/`: menus, badges and selection;
- `/scoped`: a global filter;
- `/property-mapping`: path maps and includes;
- `/access`: access policies and observers.

## Claude Code skills

The `maviray-grid` plugin teaches Claude Code how to use MaviGrid, and `maviray-material` covers the Material components
it builds on. Both come from the Maviray marketplace
([Maviray/claude-plugins](https://github.com/Maviray/claude-plugins)). To give your whole team the skills, commit this
to your project's `.claude/settings.json`:

```json
{
  "extraKnownMarketplaces": {
    "maviray": { "source": { "source": "github", "repo": "Maviray/claude-plugins" } }
  },
  "enabledPlugins": {
    "maviray-material@maviray": true,
    "maviray-grid@maviray": true
  }
}
```

Each developer then trusts the folder when Claude Code asks, and installs the plugins once in their clone:

```
claude plugin install maviray-material@maviray --scope project
claude plugin install maviray-grid@maviray --scope project
```

The skills follow this repo's `main` branch, which matches the latest released packages. `claude plugin update
maviray-grid@maviray` picks up a newer version. The skill sources are in
[plugins/maviray-grid](https://github.com/Maviray/blazor-ui-kit-pro/tree/main/plugins/maviray-grid).

## Building from source

You need the .NET 10 SDK (see `global.json`) and Node.js with npm: the grid and sample projects rebuild their Tailwind
CSS after each build.

Restoring needs the GitHub token from [Installation](#installation). This repository's `nuget.config` names the feed
`github`, and NuGet finds the token by that name, so run step 2 with `--name github`. NuGet won't register the same feed
twice, so if you've already registered it under another name, set the environment variable
`NuGetPackageSourceCredentials_github` to `Username=YOUR_GITHUB_USERNAME;Password=YOUR_TOKEN` instead.

```bash
dotnet build Maviray.Blazor.Components.Pro.slnx
dotnet test
dotnet run --project src/Maviray.Blazor.Components.Pro.Grid.Sample
```

`dotnet test` runs all three test projects. The last command starts the [sample app](#sample-app).

## Status

The library is under active development and still below 1.0, so an update can contain breaking changes. Each
release's changes are in its [release notes](https://github.com/Maviray/blazor-ui-kit-pro/releases).

## Contributing

Issues and pull requests are welcome.

## License

[MIT](https://github.com/Maviray/blazor-ui-kit-pro/blob/main/LICENSE.txt). You can use, modify and distribute the
library for free, including commercially. Keep the copyright notice (Copyright (c) 2026 Maviray) and the license text
in copies of the library.

# blazor-ui-kit-pro

`MaviGrid` is a server-paged Blazor data grid for large tables. Every page, sort or filter change loads only the rows of
the page being shown, with the sort and filters applied in the database. Access policies decide before every fetch who
may read which rows, and fetch observers record what was shown.

| Package | Contents |
|---|---|
| `Maviray.Blazor.Components.Pro.Core` | Grid models and attributes, the data factory, access policies, fetch observers, DI |
| `Maviray.Blazor.Components.Pro.Grid.EntityFramework` | The EF Core data source |
| `Maviray.Blazor.Components.Pro.Grid.UI` | The `MaviGrid` component |

## How a page is fetched

```
MaviGrid ──(MaviGridQuery, CancellationToken)──► your FetchData
  IGridDataFactory.BuildPageAsync
    1. access policies             deny → a column-only "access denied" page, no database call
    2. one combined filter         column filters AND grid filter AND global filters AND policy row filters
    3. IGridDataSource.CountAsync
    4. IGridDataSource.GetPageAsync   skipped when the count is 0; ORDER BY … key, LIMIT/OFFSET, values as parameters
    5. map rows to the model, format cells with cached getters
    6. fetch observers             the keys of the rows loaded
  ◄── MaviGridCollection<TKey>
```

What keeps it fast on large tables:

- **Only the page is loaded.** The EF source runs `COUNT` and one `ORDER BY … OFFSET/FETCH` query, without change
  tracking, on a short-lived context.
- **Stable paging.** The order always ends with the primary key, so rows with equal sort values never repeat or go
  missing between pages.
- **One query plan per grid.** Filter values are sent as SQL parameters, not literals.
- **Cheap rows.** Column metadata, compiled property getters and the entity-to-model mapping are built once per model
  type; column lists are cached per grid. Cells are value types.
- **Cheap rendering.** The grid re-renders only when what it shows changes. Typing in a filter or opening a menu doesn't
  redraw the rows, and each row redraws only when its data, selection or menu changes.
- **Nothing wasted.** A newer fetch cancels the one in flight, and the grid can skip the prerender fetch.

## Quick start

**1. Register services**

```csharp
builder.Services.AddMaviComponents();
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(connectionString)); // or AddPooledDbContextFactory

builder.Services.AddMaviGrid()
    .AddEntityFrameworkDataSource<AppDbContext>();
```

**2. Describe the columns on a grid model**

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

**3. Show the grid**

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
prerendering on, each page view fetches twice. Set `FetchWhilePrerendering = false` so the audit records one view.

## Global filters

`IGridGlobalFilterProvider` adds a synchronous constraint to every grid of every type, typically "not soft-deleted".
It only narrows rows and never grants access. Register it with `.AddGlobalFilter<T>()`. For tenant isolation across
every query, not only grids, also consider EF Core query filters.

## `MaviGrid<TKey>`

| Parameter | Type | Purpose |
|---|---|---|
| `FetchData` (required) | `Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<TKey>>>` | load one page |
| `Parameters` | `MaviGridParameters` | appearance and behaviour |
| `ConstructMainContextMenu` | `Func<Task<IEnumerable<IMaviContextMenuItem>>>` | header menu; built at start and on `RefreshAsync` |
| `OnMainContextMenuButtonClick` | `EventCallback<IMaviContextMenuItem>` | header menu item clicked |
| `OnRowContextMenuClick` | `EventCallback<IMaviRowContextMenuItem<TKey>>` | row action clicked |
| `OnCellClick` | `EventCallback<CellClickParameters<TKey>>` | cell clicked (`RowKey`, `ColumnKey`, `CellBehavior`) |
| `OnFetchError` | `EventCallback<Exception>` | a fetch failed, for example to hand it to an error boundary |
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

`GridDataFactoryOptions.MaxPageSize` (default 100) caps the page size the factory loads. The pager shows the page size
the server applied, even if it isn't one of the `PageSizeOptions`.

## Host setup

```html
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Pro.Grid.UI/css/maviray.grid.min.css" />
<link rel="stylesheet" href="_content/Maviray.Blazor.Components.Material/css/maviray.material.min.css" />
<link rel="stylesheet" href="https://cdn.lineicons.com/5.0/lineicons.css" />
<script src="_content/Maviray.Blazor.Components.Material/js/maviJsInterop.js"></script>
```

The pager's next-page icon, `lni-chevron-right`, is in LineIcons Pro. The free LineIcons 5.0 stylesheet above doesn't
have it, so there the button shows no arrow (its tooltip still works). Load a LineIcons Pro stylesheet to see it.

Render the grid in an interactive render mode. The sample app (`src/Maviray.Blazor.Components.Pro.Grid.Sample`) shows:

- `/` — menus, badges and selection;
- `/scoped` — a global filter;
- `/property-mapping` — path maps and includes;
- `/access` — access policies and observers.

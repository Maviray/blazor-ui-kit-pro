# MaviGrid recipes

One recipe per feature, each the smallest code that does it. Every recipe assumes the setup in `SKILL.md` and this
page shape:

```razor
@inject IGridDataFactory<Order, OrderGridModel, int> GridData

<MaviGrid TKey="int" FetchData="FetchPageAsync" />

@code {
    private Task<MaviGridCollection<int>> FetchPageAsync(MaviGridQuery query, CancellationToken cancellationToken) =>
        GridData.BuildPageAsync(query, Options, cancellationToken);   // Options: a static GridFetchOptions, or omit
}
```

The examples use `Order` (Id, Number, Status, Total, Margin, IsPaid, CreatedOn, Notes, Customer) and `Customer` (Id,
Name, Phone, Region).

## Contents

Sorting, paging, filtering · Row actions · Header menu · Cell clicks · Selection · Look and texts · Badges and
formatting · Related data · Narrowing rows · States · Access and audit · Fast paging · Your own service · Custom data
source · The grid model

## Sorting, paging, filtering

```razor
<MaviGrid TKey="int" FetchData="FetchPageAsync" Parameters="_parameters" />

@code {
    private readonly MaviGridParameters _parameters = new()   // a field: created once
    {
        SortColumnKey = "CreatedOn",        // model property name
        SortOrder = SortOrder.Descending,
        PageSize = 25,
        PageSizeOptions = [25, 50, 100],
        EnableFilters = true,
        FilterDebounceMilliseconds = 300
    };
}
```

- Only columns with a data item path can be sorted and filtered (see The grid model); others show no button or box.
- Filters: text contains; numbers, dates, ids equal; booleans and enums match their display text. A value the column
  can't hold (`abc`, `99999999999` for an `int`) matches no rows. Values reach SQL as parameters.

## Row actions

```csharp
// OrderGridModel
public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() =>
[
    new MaviRowContextMenuItem<int> { Key = Id, Title = "Edit", Icon = "lni lni-pencil-1",
        ThemeColorScheme = ThemeColorScheme.Primary, ElementVariant = ElementVariant.Text, ElementSize = ElementSize.Small },
    new MaviRowContextMenuItem<int> { Key = Id, Title = "Delete", Icon = "lni lni-trash-3",
        ThemeColorScheme = ThemeColorScheme.Alert, ElementVariant = ElementVariant.Text, ElementSize = ElementSize.Small }
];
```

```razor
<MaviGrid @ref="_grid" TKey="int" FetchData="FetchPageAsync" Parameters="_parameters" OnRowContextMenuClick="OnRowActionAsync" />

@code {
    private MaviGrid<int>? _grid;
    private readonly MaviGridParameters _parameters = new() { TableRowContextMenuDisplayStyle = TableRowContextMenuDisplayStyle.Icons };

    private async Task OnRowActionAsync(IMaviRowContextMenuItem<int> action)
    {
        // action.Key is the row key. Check the user may do this here; the grid doesn't.
        if (action.Title == "Delete") { await Orders.DeleteAsync(action.Key); await _grid!.RefreshAsync(); }
    }
}
```

`DropDown` (the default) puts the actions in a ⋯ menu per row. Icons are LineIcons 5 classes (`lni lni-…`); there is
no `lni-chevron-right` in LineIcons 5.

## Header menu

```razor
<MaviGrid @ref="_grid" TKey="int" FetchData="FetchPageAsync"
          ConstructMainContextMenu="BuildMenuAsync" OnMainContextMenuButtonClick="OnMenuItemAsync" />

@code {
    private MaviGrid<int>? _grid;

    // Built at start and on RefreshAsync(); return [] for no menu.
    private static Task<IEnumerable<IMaviContextMenuItem>> BuildMenuAsync() =>
        Task.FromResult<IEnumerable<IMaviContextMenuItem>>(
        [
            new MaviContextMenuItem { Key = "export", Title = "Export to CSV" },
            new MaviContextMenuItem { Key = "refresh", Title = "Refresh" },
            new MaviContextMenuItem { Key = "archive", Title = "Archive", Disabled = true }
        ]);

    private async Task OnMenuItemAsync(IMaviContextMenuItem item)
    {
        if (item.Key == "refresh") await _grid!.RefreshAsync();
    }
}
```

## Cell clicks

```csharp
// OrderGridModel: Navigational shows the column bold, like a link
[GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)]
public string Number { get; set; } = "";
```

```razor
<MaviGrid TKey="int" FetchData="FetchPageAsync" OnCellClick="OnCellClick" />

@code {
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private void OnCellClick(CellClickParameters<int> cell)
    {
        if (cell.CellBehavior == MaviGridColumnCellBehavior.Navigational) Navigation.NavigateTo($"/orders/{cell.RowKey}");
    }
}
```

Without `OnCellClick`, cells register no click handlers.

## Selection

```razor
<MaviGrid @ref="_grid" TKey="int" FetchData="FetchPageAsync" Parameters="_parameters" />
<MaviButton Title="Delete selected" OnClick="DeleteSelectedAsync" />

@code {
    private MaviGrid<int>? _grid;
    private readonly MaviGridParameters _parameters = new() { DisplaySelectColumn = true };

    private async Task DeleteSelectedAsync(MouseClickEventArgs _)
    {
        var keys = _grid!.SelectedKeys.ToArray();   // kept across pages; check access again before acting
        await Orders.DeleteAsync(keys);
        _grid.ClearSelection();
        await _grid.RefreshAsync();                // fresh count and page
    }
}
```

`_grid.SetColumnVisible("Notes", false)` hides a column; `HiddenColumnKeys` lists hidden ones. Users can do the same
with the column dialog (sliders button in the pager).

## Look and texts

```csharp
private readonly MaviGridParameters _parameters = new()
{
    Striped = true,
    Condensed = true,
    MaxCellCharsToDisplay = 40,                 // "..." after 40 chars; tooltip has the full text
    RowsPerPageText = "Zeilen pro Seite",
    SelectNumberOfRowsToDisplayText = "Anzahl der Zeilen",
    GoToPreviousPageText = "Zurück",
    GoToNextPageText = "Weiter",
    FilterPlaceholderText = "Suchen...",
    ContentMenuTitle = "Aktionen",
    NoDataAvailableTitle = "Keine Daten",
    AccessDeniedTitle = "Kein Zugriff",
    FetchErrorTitle = "Daten konnten nicht geladen werden"
};
```

`Class` and `Style` on `<MaviGrid>` style its outer element. Use the `material-css-styling` skill for classes.

## Badges and formatting

```csharp
// OrderGridModel
[Display(Name = "Status")] [GridDataProperty(3)] [GridAdvancedDisplay]   // badge
public OrderStatus Status { get; set; }                                 // text from [Display] on each enum member

[Display(Name = "Paid")] [GridDataProperty(4)] [GridBooleanDisplayValues("Paid", "Open")]
public bool IsPaid { get; set; }

[Display(Name = "Created")] [GridDataProperty(5)] [GridDateTimeFormat("dd MMM yyyy")]
public DateTime CreatedOn { get; set; }
```

```csharp
// The page: badge colour per raw value, keyed by column key
private static readonly GridFetchOptions<Order, OrderGridModel> Options = new()
{
    CellThemes = new Dictionary<string, Func<object?, ThemeColorScheme>>
    {
        ["Status"] = value => value switch
        {
            OrderStatus.Late => ThemeColorScheme.Alert,
            OrderStatus.Shipped => ThemeColorScheme.Success,
            _ => ThemeColorScheme.Info
        }
    }
};
```

Cells are formatted on the server in the invariant culture.

## Related data

Values from related entities: load them, set them in `ConstructFrom`, and map their paths so they stay sortable and
filterable.

```csharp
// OrderGridModel
[Display(Name = "Customer")] [GridDataProperty(6)] public string? CustomerName { get; set; }
[Display(Name = "Region")] [GridDataProperty(7)] public string RegionOrNone { get; set; } = "";
[Display(Name = "Margin %")] [GridDataProperty(8)] public decimal MarginPercent { get; set; }   // computed: not sortable

public void ConstructFrom(Order order)
{
    CustomerName = order.Customer?.Name;
    RegionOrNone = order.Customer?.Region?.Name ?? "None";
    MarginPercent = order.Total == 0 ? 0 : Math.Round(order.Margin / order.Total * 100, 1);
}
```

```csharp
private static readonly GridFetchOptions<Order, OrderGridModel> Options = new()
{
    Include = q => q.Include(o => o.Customer).ThenInclude(c => c!.Region),
    PropertyPathMap = new GridPropertyPathMap<Order, OrderGridModel>()
        .Map(m => m.CustomerName, "Customer.Name")
        .Map(m => m.RegionOrNone, "Customer.Region.Name", nullDefault: "None")   // a missing value sorts/filters as "None"
};
```

For a single related entity, `[GridNavProperty]` is simpler: it includes the navigation itself (see The grid model).
Create the options once (static): the factory caches columns per path map instance.

## Narrowing rows

```csharp
// One grid: AND-ed with the user's filters; limits the count and every page
private static readonly GridFetchOptions<Order, OrderGridModel> Options = new() { Filter = o => !o.IsPaid };
```

```csharp
// Every grid of a kind (soft delete, tenant): a global filter. Not for permissions: use an access policy.
public sealed class SoftDeleteFilter : IGridGlobalFilterProvider
{
    public Expression<Func<TDataItem, bool>>? GetFilter<TDataItem>() where TDataItem : class =>
        typeof(ISoftDeletable).IsAssignableFrom(typeof(TDataItem))
            ? (Expression<Func<TDataItem, bool>>)GridFilterExpression.ForInterface(
                typeof(TDataItem), (Expression<Func<ISoftDeletable, bool>>)(x => !x.IsDeleted))
            : null;
}
// Program.cs: .AddGlobalFilter<SoftDeleteFilter>()
```

## States

```razor
<MaviGrid TKey="int" FetchData="FetchPageAsync" OnFetchError="OnFetchError">
    <NoDataContent><span>No orders yet. <a href="/orders/new">Create one</a></span></NoDataContent>
    <FetchErrorContent><span>Orders are unavailable. Try again shortly.</span></FetchErrorContent>
    <AccessDeniedContent><span>Ask your manager for access.</span></AccessDeniedContent>
</MaviGrid>

@code {
    private void OnFetchError(Exception exception) => _error = exception.Message;   // the grid also logs it
}
```

- Defaults: empty state with `NoDataAvailableTitle` and a clear-filters button; error state with `FetchErrorTitle`
  and Retry; denied state with the policy's message or `AccessDeniedTitle` (headers and ⋯ menu kept, no filters,
  selection or pager); a spinner over the rows while fetching.
- Loading placeholder instead of a prerender query: `FetchWhilePrerendering = false` plus `<PrerenderContent>`.

## Access and audit

```csharp
// Before every fetch of an Order grid (or a subtype). Reads the user; the library knows no roles.
public sealed class OrderAccessPolicy(AuthenticationStateProvider auth) : GridAccessPolicy<Order>
{
    protected override async ValueTask<GridAccessDecision<Order>> EvaluateAsync(GridAccessRequest request, CancellationToken ct)
    {
        var user = (await auth.GetAuthenticationStateAsync()).User;

        if (!user.IsInRole("Sales")) return GridAccessDecision<Order>.Deny("Only sales can see orders.");

        var region = user.FindFirst("region")?.Value;
        var decision = GridAccessDecision<Order>.Allow(o => o.Customer!.Region!.Code == region);   // only these rows

        // Hidden columns vanish from the page; their sort and filter are ignored, so values can't be guessed.
        return user.IsInRole("Manager") ? decision : decision.WithHiddenColumns("Margin", "MarginPercent");
    }
}
```

```csharp
// Audit: told what every fetch loaded. Observers run inside the fetch: exceptions fail it.
public sealed class OrderAuditObserver(IAuditQueue queue) : IGridFetchObserver
{
    public ValueTask OnRowsLoadedAsync<TDataItem, TKey>(GridRowsLoaded<TKey> loaded, CancellationToken ct)
        where TDataItem : class where TKey : IEquatable<TKey>
    {
        queue.Enqueue(typeof(TDataItem).Name, loaded.Request.AccessTag, loaded.RowKeys, loaded.PageNumber);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAccessDeniedAsync<TDataItem>(GridAccessDenied denied, CancellationToken ct) where TDataItem : class
    {
        queue.Enqueue(typeof(TDataItem).Name, denied.Request.AccessTag, denied.Message);
        return ValueTask.CompletedTask;
    }
}
```

```csharp
// Program.cs
builder.Services.AddMaviGrid(o => o.RequireExplicitAllow = true)   // deny what no policy allows
    .AddEntityFrameworkDataSource<AppDbContext>()
    .AddAccessPolicy<OrderAccessPolicy>()
    .AddFetchObserver<OrderAuditObserver>();

// A page: tell grids over the same type apart for policies and observers
private static readonly GridFetchOptions<Order, OrderGridModel> Options = new() { AccessTag = "orders-admin" };
```

- Policies run in registration order: the first deny wins, allow row filters are AND-ed, hidden columns add up.
  `NotApplicable().WithHiddenColumns(...)` hides columns without allowing rows.
- Return `NotApplicable()` for grids a policy doesn't govern (check `request.AccessTag` or `request.ModelType`).
- A rule over an interface: `GridAccessPolicy<ITenantScoped>` covers every implementing entity, filter rebound to
  each entity's own property.
- A policy that throws or returns `null` fails the fetch. Call `RefreshAsync()` when the user's access changes.
- In Blazor Server a scope is the circuit: don't cache grants for its whole life.

## Fast paging

```razor
<MaviGrid TKey="int" FetchData="FetchPageAsync" PrerenderedPageKey="orders" />

@code {
    private static readonly GridFetchOptions<Order, OrderGridModel> Options = new()
    {
        // SELECT only the shown columns; .Load what ConstructFrom / GetContextActions read that isn't a column
        Projection = new GridProjection<Order>().Load(o => o.Margin)
    };
}
```

- **Count reuse (automatic).** Page, size or sort changes send `KnownTotalRows`; the factory skips `COUNT`. It
  counts again on filter change, `RefreshAsync()`, or a page shorter than the total predicts. Turn off with
  `AddMaviGrid(o => o.ReuseTotalRows = false)`.
- **Keyset paging (automatic with EF).** Next / previous page sends a `Cursor`; the EF source seeks
  (`WHERE (sort, key) > (…)`) instead of `OFFSET`. Needs a sort on a non-null column of the entity itself (or a path
  map `nullDefault`) and a non-converted key; otherwise it skips as before.
- **Projection.** Unlisted members read as their default; types on the paths need a parameterless constructor and
  settable members; no paths through collections.
- **Prerendered page.** Key unique per grid on the page; reused only for the same initial page, size, sort and filters;
  restored row actions come back as `MaviRowContextMenuItem<TKey>`. One query and one audit entry per view.

## Your own service

When pages come from your own service (DTOs, your own authorization), skip `IGridDataFactory`:

```razor
@inject OrderService Orders

<MaviGrid TKey="Guid" FetchData="FetchPageAsync" Parameters="_parameters" />

@code {
    private async Task<MaviGridCollection<Guid>> FetchPageAsync(MaviGridQuery query, CancellationToken ct)
    {
        // Only DTO-backed columns are sortable/filterable: these are the only keys the grid sends.
        var request = new OrderPageRequest(query.PageNumber, query.PageSize, query.SortColumnKey ?? "Number", query.SortOrder,
            query.Filters.ToDictionary(f => f.ColumnKey, f => f.Value), query.KnownTotalRows, query.Cursor);

        var result = await Orders.GetPageAsync(request, ct);
        if (result.IsForbidden) return GridPageBuilder<OrderDto, OrderRow, Guid>.Denied("You can't view orders.");

        var page = result.Value;
        return GridPageBuilder<OrderDto, OrderRow, Guid>.Build(
            new GridLoadedPage<OrderDto>(page.Items, page.Page, page.PageSize, page.TotalCount)
            {
                NextPageCursor = page.NextCursor,         // optional: keyset
                PreviousPageCursor = page.PreviousCursor,
                HiddenColumns = page.HiddenColumns        // optional: column access (also ignore their sort/filter)
            },
            query);
    }
}
```

```csharp
// Inside the service, after scope and filters (rows is an IQueryable<Order>):
var total = request.KnownTotal ?? await rows.CountAsync(ct);   // recount if the page comes back short
var loaded = await new GridKeyset<Order>(
        [new GridOrderKey(request.SortBy, request.SortOrder), new GridOrderKey(nameof(Order.Id), request.SortOrder)])
    .LoadPageAsync(rows, request.Cursor, (page - 1) * request.PageSize, request.PageSize,
        // The load returns entities (the cursors read their order keys). Projecting is fine if the keys stay:
        (q, token) => q.SelectPaths(["Id", "Number", "Total", request.SortBy]).ToListAsync(token),
        ct);
var items = loaded.Items.Select(o => new OrderDto(o.Id, o.Number, o.Total)).ToList();
// loaded.FirstRowCursor (previous page), loaded.LastRowCursor (next page; send null on the last page)
```

- `OrderRow : IGridCollectionModel<OrderDto, Guid>` carries the attributes; private setters set in `ConstructFrom`
  are fine. `GridPageBuilder<…>.Columns()` lists which keys are sortable and filterable.
- `GridKeyset` keys must be non-null numbers, strings, bools, Guids, enums stored as numbers, dates or times (a `Guid`
  key is compared in the database's own order). End with a unique key. A cursor from another order is ignored.
- Policies and observers registered with `AddMaviGrid` don't run on this path: the service is the gate.

## Custom data source

For a non-EF store, implement `IGridDataSource<T>` and register it (`.AddDataSource(typeof(MySource<>))` or
`services.AddScoped<IGridDataSource<Order>, OrderSource>()`):

- `CountAsync(query, ct)`: rows matching `query.Filter` (an expression; apply it to an `IQueryable`).
- `GetPageAsync(query, ct)`: apply `IncludePaths`/`Include` (skip them when `SelectPaths` is set), `Filter`, then order by
  `query.Sort` (`OrderByDynamic(path, order, nullDefault)`) and break ties with a unique key
  (`ThenByDynamic("Id", order)`); with no sort, order by the key. Then `Skip`/`Take`, and `.SelectPaths(query.SelectPaths)`
  when set. Stateless: open and close connections per call.
- Keyset: also implement `IGridKeysetDataSource<T>.GetKeysetPageAsync` returning
  `GridKeyset<T>.LoadPageAsync(filtered, query.Cursor, query.Skip, query.Take, …)` in the same order.

## The grid model

```csharp
public sealed class OrderGridModel : IGridCollectionModel<Order, int>
{
    [GridDataProperty(1)] public int Id { get; set; }                       // same-named: copied, sortable, filterable

    [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)]          // bold, link-like (OnCellClick)
    public string Number { get; set; } = "";

    [Display(Name = "Status")] [GridDataProperty(3)] [GridAdvancedDisplay]
    public OrderStatus Status { get; set; }

    [Display(Name = "Total")] [GridDataProperty(4)] public decimal Total { get; set; }

    [Display(Name = "Total incl. VAT")] [GridDataProperty(5)]               // computed: shown, not sortable
    public decimal TotalWithVat { get; set; }

    [GridNavProperty(6)]                                                     // named like Order.Customer: included,
    public CustomerColumns? Customer { get; set; }                          // its nested columns keyed "Customer.Name"

    public int Key => Id;

    public void ConstructFrom(Order order) => TotalWithVat = Math.Round(order.Total * 1.2m, 2);   // after the copy

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

public sealed class CustomerColumns
{
    [GridNestedProperty("Customer")] public string Name { get; set; } = "";
    [GridNestedProperty("Phone")] public string? Phone { get; set; }
}
```

- Columns: properties with `[GridDataProperty]`, ordered by sequence, then declaration. Key = property name.
- Several models can read one entity (a compact list and a full admin view).
- A filter or sort on a key that isn't a sortable column is ignored, so a request can't reach hidden data.

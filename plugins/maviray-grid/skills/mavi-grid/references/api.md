# MaviGrid API reference

Everything the grid packages expose to app code. Recipes for each part: `recipes.md`.

## Namespaces

| Namespace | Types |
|---|---|
| `Maviray.Blazor.Components.Pro.Grid.UI.Components` | `MaviGrid<TKey>`, `MaviGridParameters` |
| `Maviray.Blazor.Components.Pro.Core.Models.Grid` | `MaviGridQuery`, `MaviGridColumnFilter`, `MaviGridCursor`, `MaviGridCollection<TKey>`, `MaviGridColumn`, `MaviGridRow<TKey>`, `MaviGridCell`, `GridFetchOptions<TDataItem, TModel>`, `GridPropertyPathMap<TDataItem, TModel>`, `GridProjection<TDataItem>`, `GridLoadedPage<T>`, `GridKeysetPage<T>` |
| `Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu` | `MaviRowContextMenuItem<TKey>`, `CellClickParameters<TKey>` |
| `Maviray.Blazor.Components.Pro.Core.Models` | `MaviContextMenuItem` |
| `Maviray.Blazor.Components.Pro.Core.Interfaces` | `IGridCollectionModel<TDataItem, TKey>`, `IMaviContextMenuItem` |
| `Maviray.Blazor.Components.Pro.Core.Interfaces.Grid` | `IGridDataFactory<,,>`, `IGridDataSource<T>`, `IGridKeysetDataSource<T>`, `GridDataSourceQuery<T>`, `GridSort`, `IGridGlobalFilterProvider`, `IMaviRowContextMenuItem<TKey>` |
| `Maviray.Blazor.Components.Pro.Core.Access` | `IGridAccessPolicy`, `GridAccessPolicy<TEntity>`, `GridAccessDecision<T>`, `GridAccessRequest`, `IGridFetchObserver`, `GridRowsLoaded<TKey>`, `GridAccessDenied`, `GridFilterExpression` |
| `Maviray.Blazor.Components.Pro.Core.Attributes.Grid` | the column attributes |
| `Maviray.Blazor.Components.Pro.Core.Enums.Grid` | `MaviGridColumnCellBehavior`, `MaviGridFilterCriteria`, `MaviGridPropertyType` |
| `Maviray.Blazor.Components.Pro.Core.Services` | `GridPageBuilder<TDataItem, TModel, TKey>` |
| `Maviray.Blazor.Components.Pro.Core.Extensions` | `AddMaviGrid`, `MaviGridBuilder`, `OrderByDynamic`, `ThenByDynamic`, `SelectPaths`, `GridKeyset<T>`, `GridOrderKey` |
| `Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions` | `AddEntityFrameworkDataSource<TDbContext>()` |
| `Maviray.Blazor.Components.Core.Enums` | `SortOrder`, `ThemeColorScheme`, `ElementVariant`, `ElementSize`, `TableRowContextMenuDisplayStyle` |

## `MaviGrid<TKey>` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `TKey` | type parameter | — | Row key type (`int`, `long`, `Guid`, `string`, …), `IEquatable<TKey>`. Always set it: `TKey="int"`. |
| `FetchData` | `Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<TKey>>>` | required | Loads one page. Called at start, on every page / size / sort / filter change, and on `RefreshAsync()`. Pass the token on. |
| `Parameters` | `MaviGridParameters` | `new()` | Look and behaviour (below). Initial sort and page size apply once; then the user's choices win. |
| `ConstructMainContextMenu` | `Func<Task<IEnumerable<IMaviContextMenuItem>>>?` | `null` | Builds the ⋯ header menu; called at start and on `RefreshAsync()`. No items, no button. |
| `OnMainContextMenuButtonClick` | `EventCallback<IMaviContextMenuItem>` | — | A header menu item was clicked. |
| `OnRowContextMenuClick` | `EventCallback<IMaviRowContextMenuItem<TKey>>` | — | A row action was clicked; its `Key` is the row key. |
| `OnCellClick` | `EventCallback<CellClickParameters<TKey>>` | — | A cell was clicked. Cells handle clicks only when this is set. |
| `OnFetchError` | `EventCallback<Exception>` | — | `FetchData` threw (not for fetches the grid cancelled). |
| `NoDataContent` | `RenderFragment?` | `null` | Replaces the empty state. |
| `AccessDeniedContent` | `RenderFragment?` | `null` | Replaces the access-denied message. |
| `FetchErrorContent` | `RenderFragment?` | `null` | Replaces the fetch-error message and Retry button. |
| `PrerenderContent` | `RenderFragment?` | `null` | Shown while prerendering when `FetchWhilePrerendering` is false (default: skeleton). |
| `PrerenderedPageKey` | `string?` | `null` | Unique per grid on a page: the prerendered page is carried into the interactive grid, which doesn't fetch it again. |
| `Id`, `Class`, `Style` | `string?` | generated / `null` | Id, extra classes and style of the grid's container. |
| `Title` | `string?` | `null` | Inherited; not used by MaviGrid. |

## `MaviGrid<TKey>` methods and properties (through `@ref`)

| Member | Type | Description |
|---|---|---|
| `RefreshAsync()` | `Task` | Fetch the current page again with a fresh count; rebuild the header menu. Call after data or access changes. |
| `ClearSelection()` | `void` | Unselect every row. |
| `SetColumnVisible(string columnKey, bool visible)` | `void` | Hide or show a column (as the user's column dialog does). |
| `SelectedKeys` | `IReadOnlyCollection<TKey>` | Selected row keys, kept across pages. |
| `HiddenColumnKeys` | `IReadOnlyCollection<string>` | Columns hidden by the user or `SetColumnVisible`. |
| `HandleOutsideClick(string)` | `void` | JS callback that closes menus; not for app code. |

## `MaviGridParameters`

| Property | Type | Default | Description |
|---|---|---|---|
| `SortColumnKey` | `string?` | `null` | Column of the first sort (the model property name); `null`: primary key order. |
| `SortOrder` | `SortOrder` | `Ascending` | Direction of the first sort. |
| `PageSize` | `int` | `10` | Rows per page at start; capped by `MaxPageSize`. |
| `PageSizeOptions` | `IReadOnlyList<int>` | `[10, 25, 50, 100]` | Sizes offered in the pager. |
| `EnableFilters` | `bool` | `false` | Filter box under each filterable column. |
| `FilterDebounceMilliseconds` | `int` | `400` | Pause after typing before fetching. |
| `FilterPlaceholderText` | `string` | `"Search..."` | Filter box placeholder. |
| `DisplaySelectColumn` | `bool` | `false` | Checkbox column; read `SelectedKeys`. |
| `TableRowContextMenuDisplayStyle` | `TableRowContextMenuDisplayStyle` | `DropDown` | Row actions in a ⋯ drop-down or as `Icons`. |
| `Striped` | `bool` | `false` | Alternating row background. |
| `Condensed` | `bool` | `false` | Tighter cells, smaller text. |
| `MaxCellCharsToDisplay` | `int` | `0` | Cut cell text with "..." (0: no limit); full text in the tooltip. |
| `FetchWhilePrerendering` | `bool` | `true` | `false`: placeholder while prerendering, one fetch once interactive. |
| `StretchColumns` | `IReadOnlyDictionary<string, int>?` | `null` | Column key → weight: these columns share the free width, replacing the model's `[GridStretch]` marks. `{}`: none stretch. Keys use the dictionary's comparer; unknown keys are ignored; weights ≥ 1 (else `ArgumentOutOfRangeException`). Assign a new dictionary to change it. |
| `FillWidth` | `bool` | `true` | Fill the parent's width; `false`: shrink to the columns (stretching then has no visible effect). |
| `ContentMenuTitle` | `string` | `"Actions"` | Tooltip of the ⋯ buttons. |
| `NoDataAvailableTitle` | `string` | `"No data available to display"` | Empty state text. |
| `AccessDeniedTitle` | `string?` | `"You do not have access to this data"` | Denied text when the policy gives none. |
| `FetchErrorTitle` | `string?` | `"Data could not be loaded"` | Error state text. |
| `RowsPerPageText` | `string` | `"Rows per page"` | Label before the page size selector. |
| `SelectNumberOfRowsToDisplayText` | `string` | `"Select number of rows to display"` | Page size selector tooltip. |
| `GoToPreviousPageText`, `GoToNextPageText` | `string` | `"Go to previous page"`, `"Go to next page"` | Pager button tooltips. |
| `PaginationHorizontalAlignment`, `BackdropOpacity`, `ZIndex` | — | — | Inherited from `TableParameters`; not used by MaviGrid. |

## `GridFetchOptions<TDataItem, TModel>` (passed to `BuildPageAsync`; create once per grid)

| Property | Type | Description |
|---|---|---|
| `Filter` | `Expression<Func<TDataItem, bool>>?` | Row constraint for this grid, AND-ed with everything; limits count and pages. |
| `Include` | `GridIncludeConfigurator<TDataItem>?` (`IQueryable<T> → IQueryable<T>`) | Shapes the query first: `Include(...).ThenInclude(...)`, `IgnoreQueryFilters()`. |
| `PropertyPathMap` | `GridPropertyPathMap<TDataItem, TModel>?` | `.Map(m => m.Prop, "Nav.Path", nullDefault?)`: data item paths for columns with no same-named member. |
| `Projection` | `GridProjection<TDataItem>?` | Load only the shown columns plus `.Load(x => x.Member)` members. |
| `CellThemes` | `IReadOnlyDictionary<string, Func<object?, ThemeColorScheme>>?` | Badge colour per raw value for `[GridAdvancedDisplay]` columns, keyed by column key. |
| `AccessTag` | `string?` | Passed to policies and observers to tell grids over the same type apart. |

## Registration

| Call | Description |
|---|---|
| `AddMaviGrid(Action<GridDataFactoryOptions>?)` | Registers `IGridDataFactory<,,>` (scoped) and options, validated at startup. |
| option `RequireExplicitAllow` (`false`) | Deny every fetch no policy allows (fail closed). |
| option `MaxPageSize` (`100`) | Largest page loaded. |
| option `ReuseTotalRows` (`true`) | Skip the count when only page, size or sort changed. |
| `.AddEntityFrameworkDataSource<TDbContext>()` | EF Core reads for every grid; needs `AddDbContextFactory<TDbContext>()`. |
| `.AddDataSource(Type openGenericType, ServiceLifetime)` | Your own `IGridDataSource<>` for every type. |
| `.AddAccessPolicy<T>()` / `(instance)` | An `IGridAccessPolicy` (by type: scoped, added once; by instance: singleton). |
| `.AddFetchObserver<T>()` / `(instance)` | An `IGridFetchObserver`. |
| `.AddGlobalFilter<T>()` / `(instance)` | An `IGridGlobalFilterProvider`. |

## Grid model: `IGridCollectionModel<TDataItem, TKey>`

| Member | Description |
|---|---|
| `TKey Key { get; }` | Row identity for selection, actions and clicks. |
| `void ConstructFrom(TDataItem item)` | Sets computed columns. Same-named members are copied before it runs (Mapster). |
| `IEnumerable<IMaviRowContextMenuItem<TKey>> GetContextActions()` | The row's actions; `[]` for none. |

| Attribute | On | Effect |
|---|---|---|
| `[GridDataProperty(sequence, cellBehavior = Default)]` | property | A column, ordered by sequence then declaration. `Navigational`: bold, link-like. |
| `[Display(Name = "…")]` | property / enum member | Header text / enum cell text. |
| `[GridNavProperty(sequence, cellBehavior)]` | related-model property named like the entity's navigation | Loads that navigation; its `[GridNestedProperty]` members become columns keyed `Nav.Property`. |
| `[GridNestedProperty("Title")]` | property of the related model | A nested column with its header. |
| `[GridBooleanDisplayValues("Yes", "No")]` | bool | Cell text; also what the filter matches. |
| `[GridDateTimeFormat("dd MMM yyyy")]` | date/time | Format (invariant). Defaults: `yyyy-MM-dd HH:mm:ss`, `DateOnly` `yyyy-MM-dd`. |
| `[GridAdvancedDisplay]` | property | Badge coloured by `CellThemes`. |
| `[GridStretch(weight = 1)]` | column property, or `[GridNavProperty]` (its nested columns; a nested member's own mark wins) | Shares the free width with other stretching columns, by weight. None visible: the actions column takes the free width. Weight < 1 throws `InvalidOperationException` on the model's first page. Sets `MaviGridColumn.StretchWeight`. |

Column key = model property name (`Nav.Property` for nested). A column is sortable and filterable when it has a data
item path: a same-named entity member, or a path map entry.

## `MaviGridQuery` (what `FetchData` receives)

| Property | Type | Description |
|---|---|---|
| `PageNumber` | `int` | One-based target page; clamp to the available pages. |
| `PageSize` | `int` | Rows per page. |
| `SortColumnKey`, `SortOrder` | `string?`, `SortOrder` | Sort, or `null` for the default order. |
| `Filters` | `IReadOnlyList<MaviGridColumnFilter>` | `(ColumnKey, Criteria, Value)`, AND-ed; values in the invariant culture. Strings arrive as `Contains`, others as `Equals`, booleans as `IsTrue`/`IsFalse`. |
| `KnownTotalRows` | `int?` | Hint: current total when only page, size or sort changed; use it instead of counting. |
| `Cursor` | `MaviGridCursor?` | Hint: `(Value, Direction Next/Previous)` to seek instead of skipping rows. |

## `MaviGridCollection<TKey>` (what `FetchData` returns)

| Property | Description |
|---|---|
| `Columns` | Columns in display order (required). |
| `Rows` | `MaviGridRow<TKey>`: `Key`, `Cells` (`row.Cells[column.Index]`), `ContextActions`. |
| `PageNumber`, `PageSize`, `TotalRows`, `TotalPages` | Paging after clamping. |
| `SortColumnKey`, `SortOrder` | The applied sort. |
| `AccessDenied`, `AccessDeniedMessage` | Denied page: headers only. |
| `NextPageCursor`, `PreviousPageCursor` | Keyset cursors the grid sends back as `Cursor`. |
| `FirstRowNumber`, `LastRowNumber` | Computed row range. |

Build it with `IGridDataFactory.BuildPageAsync` or `GridPageBuilder`; don't assemble columns and cells by hand.

## Callback payloads

| Type | Members |
|---|---|
| `CellClickParameters<TKey>` | `RowKey`, `ColumnKey`, `CellBehavior` |
| `MaviContextMenuItem` (`IMaviContextMenuItem`) | `Key`, `Title`, `Css`, `Disabled` |
| `MaviRowContextMenuItem<TKey>` (`IMaviRowContextMenuItem<TKey>`) | `Key` (row key), `Title`, `Icon` (LineIcons class), `Disabled`, `ElementVariant`, `ThemeColorScheme`, `ElementSize` |

## Access and audit

| Type or member | Description |
|---|---|
| `IGridAccessPolicy.EvaluateAsync<TDataItem>(GridAccessRequest, ct)` | Runs before every fetch, in registration order. |
| `GridAccessPolicy<TEntity>` | Base class for one entity type and its subtypes (or an interface's implementations); others get `NotApplicable`. Override `EvaluateAsync(request, ct)`. |
| `GridAccessDecision<T>.NotApplicable()` / `.Allow(rowFilter?)` / `.Deny(message?)` | Not my concern / allowed, optionally only some rows / denied (first deny wins). |
| `.WithHiddenColumns(params columnKeys)` | On Allow or NotApplicable: remove those columns from the page and ignore their sort and filter. |
| `GridAccessRequest` | `DataItemType`, `ModelType`, `AccessTag`. |
| `GridFilterExpression.ForInterface(Type, LambdaExpression)` | Rebinds a filter over an interface to an entity's own properties (EF-translatable). |
| `IGridFetchObserver` | `OnRowsLoadedAsync<TDataItem, TKey>(GridRowsLoaded<TKey>, ct)` (`Request`, `RowKeys`, `PageNumber`, `PageSize`, `TotalRows`) and `OnAccessDeniedAsync<TDataItem>(GridAccessDenied, ct)` (`Request`, `Message`); both default to no-ops. |
| `IGridGlobalFilterProvider.GetFilter<TDataItem>()` | A filter for every grid, or `null` for types it doesn't govern. |

## Loading pages yourself

| Type | Description |
|---|---|
| `IGridDataFactory<TDataItem, TModel, TKey>.BuildPageAsync(query, options?, ct)` | The full pipeline (policies, filters, count, page, rows, observers). |
| `GridPageBuilder<TDataItem, TModel, TKey>` | `Build(GridLoadedPage<TDataItem>, MaviGridQuery, options?)`, `Denied(message?)`, `Columns(map?, hiddenColumns?)`. |
| `GridLoadedPage<TDataItem>(Items, PageNumber, PageSize, TotalRows)` | `{ NextPageCursor, PreviousPageCursor, HiddenColumns }` optional. |
| `GridKeyset<T>(IReadOnlyList<GridOrderKey>)` | `LoadPageAsync(source, cursor, skip, take, load, ct)` → `GridKeysetPage<T>(Items, FirstRowCursor, LastRowCursor)`; `CanSeek(Type)`, `TryCreate(keys, out keyset)`, `CursorFor(item)`. Seekable keys: non-null numbers, string, bool, Guid, enum (stored as a number), dates and times. |
| `GridOrderKey(Path, Order, NullDefault = null)` | One key of a keyset order. |
| `GridProjection<TDataItem>` | Immutable; `.Load(x => x.A.B)` or `.Load("A.B")`. |
| `QueryableExtensions` | `OrderByDynamic(path, order, nullDefault?)`, `ThenByDynamic(...)`, `SelectPaths(paths)`. |
| `IGridDataSource<T>` | `CountAsync(GridDataSourceQuery<T>, ct)`, `GetPageAsync(query, ct)`. |
| `IGridKeysetDataSource<T>` | Adds `GetKeysetPageAsync(query, ct)` → `GridKeysetPage<T>`. |
| `GridDataSourceQuery<T>` | `Filter`, `Sort` (`GridSort(Path, Order, NullDefault)`), `Skip`, `Take`, `IncludePaths`, `Include`, `SelectPaths`, `Cursor`. |

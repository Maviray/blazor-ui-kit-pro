namespace Maviray.Blazor.Components.Pro.Grid.Sample.Components.Docs;

/// <summary>One row of a reference table.</summary>
public sealed record DocRow(string Name, string Type, string Default, string Description);

/// <summary>The contents of the reference tables: everything MaviGrid and its packages expose to app code.</summary>
internal static class DocReference
{
    public static readonly DocRow[] ComponentParameters =
    [
        new("TKey", "type parameter", "—", "Type of the row key (int, long, Guid, string, …); must implement IEquatable<TKey>. Always set it explicitly: TKey=\"int\"."),
        new("FetchData", "Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<TKey>>>", "required", "Loads one page. Called when the grid starts and on every page, page size, sort and filter change, and on RefreshAsync(). Pass the token on: the grid cancels a fetch it no longer needs."),
        new("Parameters", "MaviGridParameters", "new()", "Look and behaviour (see 1.3). The initial sort and page size apply once; after that the user's choices win."),
        new("ConstructMainContextMenu", "Func<Task<IEnumerable<IMaviContextMenuItem>>>?", "null", "Builds the ⋯ menu in the header. Called when the grid starts and on RefreshAsync(). No items, no menu button."),
        new("OnMainContextMenuButtonClick", "EventCallback<IMaviContextMenuItem>", "—", "A header menu item was clicked."),
        new("OnRowContextMenuClick", "EventCallback<IMaviRowContextMenuItem<TKey>>", "—", "A row action was clicked. The item's Key is the row's key."),
        new("OnCellClick", "EventCallback<CellClickParameters<TKey>>", "—", "A cell was clicked: row key, column key and the column's cell behaviour. Cells only handle clicks when this is set."),
        new("OnFetchError", "EventCallback<Exception>", "—", "FetchData threw. Not raised for fetches the grid cancelled itself."),
        new("NoDataContent", "RenderFragment?", "null", "Replaces the built-in empty state (its text and the clear-filters button)."),
        new("AccessDeniedContent", "RenderFragment?", "null", "Replaces the access-denied message."),
        new("FetchErrorContent", "RenderFragment?", "null", "Replaces the fetch-error message and its Retry button."),
        new("PrerenderContent", "RenderFragment?", "null", "Shown while prerendering when FetchWhilePrerendering is false. Default: skeleton lines."),
        new("PrerenderedPageKey", "string?", "null", "When set (unique among the grids on a page), the page fetched while prerendering is carried into the interactive grid, which then doesn't fetch it again."),
        new("Id", "string?", "generated", "Id of the grid's container element; also prefixes the menu ids."),
        new("Class", "string?", "null", "Extra CSS classes on the grid's container."),
        new("Style", "string?", "null", "Extra inline style on the grid's container."),
        new("Title", "string?", "null", "Inherited from MaviComponentBase; not used by MaviGrid.")
    ];

    public static readonly DocRow[] ComponentMembers =
    [
        new("RefreshAsync()", "Task", "", "Fetches the current page again, counting the rows afresh, and rebuilds the header menu. Call it after you change data, or when the user's access changes."),
        new("ClearSelection()", "void", "", "Unselects every row."),
        new("SetColumnVisible(string columnKey, bool visible)", "void", "", "Hides or shows a column, as the user can in the column dialog."),
        new("SelectedKeys", "IReadOnlyCollection<TKey>", "", "Keys of the selected rows. Kept across pages."),
        new("HiddenColumnKeys", "IReadOnlyCollection<string>", "", "Keys of the columns hidden by the user or by SetColumnVisible. Kept across pages."),
        new("HandleOutsideClick(string clickedMenuId)", "void", "", "Called by the outside-click script to close open menus. Infrastructure; not for app code."),
        new("DisposeAsync()", "ValueTask", "", "Called by Blazor when the grid leaves the page: cancels a running fetch.")
    ];

    public static readonly DocRow[] GridParameters =
    [
        new("SortColumnKey", "string?", "null", "Column the first page is sorted by. null: the data source's default order (the primary key)."),
        new("SortOrder", "SortOrder", "Ascending", "Direction of that first sort."),
        new("PageSize", "int", "10", "Rows per page when the grid starts. The factory caps it at MaxPageSize."),
        new("PageSizeOptions", "IReadOnlyList<int>", "[10, 25, 50, 100]", "Page sizes offered in the pager."),
        new("EnableFilters", "bool", "false", "Shows a filter box under each column that can be filtered."),
        new("FilterDebounceMilliseconds", "int", "400", "Pause after the user stops typing in a filter before the grid fetches."),
        new("FilterPlaceholderText", "string", "\"Search...\"", "Placeholder in the filter boxes."),
        new("DisplaySelectColumn", "bool", "false", "Adds a checkbox column; read the selection with SelectedKeys."),
        new("TableRowContextMenuDisplayStyle", "TableRowContextMenuDisplayStyle", "DropDown", "Row actions in a ⋯ drop-down (DropDown) or as icon buttons (Icons)."),
        new("Striped", "bool", "false", "Alternating row background."),
        new("Condensed", "bool", "false", "Tighter cells and smaller text."),
        new("MaxCellCharsToDisplay", "int", "0", "Cuts cell text to this many characters with \"...\" (0: no limit). The full text stays in the tooltip."),
        new("FetchWhilePrerendering", "bool", "true", "false: a placeholder while prerendering and one fetch once interactive (or keep true and set PrerenderedPageKey)."),
        new("ContentMenuTitle", "string", "\"Actions\"", "Tooltip of the ⋯ menu buttons."),
        new("NoDataAvailableTitle", "string", "\"No data available to display\"", "Text of the empty state."),
        new("AccessDeniedTitle", "string?", "\"You do not have access to this data\"", "Text of the access-denied state when the policy gives no message."),
        new("FetchErrorTitle", "string?", "\"Data could not be loaded\"", "Text of the fetch-error state."),
        new("RowsPerPageText", "string", "\"Rows per page\"", "Label before the page size selector."),
        new("SelectNumberOfRowsToDisplayText", "string", "\"Select number of rows to display\"", "Tooltip of the page size selector."),
        new("GoToPreviousPageText", "string", "\"Go to previous page\"", "Tooltip of the previous-page button."),
        new("GoToNextPageText", "string", "\"Go to next page\"", "Tooltip of the next-page button."),
        new("PaginationHorizontalAlignment, BackdropOpacity, ZIndex", "—", "Right, Lighten, Forty", "Inherited from TableParameters; not used by MaviGrid.")
    ];

    public static readonly DocRow[] FetchOptions =
    [
        new("Filter", "Expression<Func<TDataItem, bool>>?", "null", "A row constraint for this grid, AND-combined with everything else. It limits the count and the page."),
        new("Include", "GridIncludeConfigurator<TDataItem>?", "null", "Shapes the query before filtering: Include(...).ThenInclude(...), IgnoreQueryFilters(), …"),
        new("PropertyPathMap", "GridPropertyPathMap<TDataItem, TModel>?", "null", "Data item paths for model columns with no same-named member, so they can be sorted and filtered."),
        new("Projection", "GridProjection<TDataItem>?", "null", "Loads only the shown columns plus the listed members, instead of whole entities with their includes."),
        new("CellThemes", "IReadOnlyDictionary<string, Func<object?, ThemeColorScheme>>?", "null", "Badge colour of each value in a [GridAdvancedDisplay] column, keyed by column key."),
        new("AccessTag", "string?", "null", "Passed to access policies and fetch observers, to tell grids over the same type apart.")
    ];

    public static readonly DocRow[] Registration =
    [
        new("AddMaviGrid(Action<GridDataFactoryOptions>?)", "MaviGridBuilder", "", "Registers IGridDataFactory<TDataItem, TModel, TKey> (scoped) and its options, validated when the app starts."),
        new("RequireExplicitAllow", "bool", "false", "Option. Deny every fetch that no access policy allows (fail closed)."),
        new("MaxPageSize", "int", "100", "Option. The largest page the factory loads."),
        new("ReuseTotalRows", "bool", "true", "Option. Skip the row count when only the page, page size or sort changed."),
        new(".AddEntityFrameworkDataSource<TDbContext>()", "MaviGridBuilder", "", "Reads every grid's rows with EF Core. Needs AddDbContextFactory<TDbContext>()."),
        new(".AddDataSource(Type openGenericType, ServiceLifetime)", "MaviGridBuilder", "", "Your own IGridDataSource<> for every data item type."),
        new(".AddAccessPolicy<T>() / .AddAccessPolicy(instance)", "MaviGridBuilder", "", "An IGridAccessPolicy that decides who may read which rows and columns."),
        new(".AddFetchObserver<T>() / .AddFetchObserver(instance)", "MaviGridBuilder", "", "An IGridFetchObserver told what every fetch loaded (audit)."),
        new(".AddGlobalFilter<T>() / .AddGlobalFilter(instance)", "MaviGridBuilder", "", "An IGridGlobalFilterProvider: a row constraint for every grid (soft delete, tenant)."),
    ];

    public static readonly DocRow[] ModelMembers =
    [
        new("Key", "TKey", "", "The row's identity: what selection, row actions and cell clicks report."),
        new("ConstructFrom(TDataItem dataItem)", "void", "", "Sets the columns that aren't copied automatically. Same-named members are copied from the data item before it runs."),
        new("GetContextActions()", "IEnumerable<IMaviRowContextMenuItem<TKey>>", "", "The row's actions, shown as icons or in a drop-down. Return [] for none.")
    ];

    public static readonly DocRow[] Attributes =
    [
        new("[GridDataProperty(sequence, cellBehavior)]", "property", "Default", "Makes the property a column, ordered by sequence. Navigational shows it bold, as a link."),
        new("[Display(Name = \"…\")]", "property, enum member", "property name", "Header text; on an enum member, the text shown in its cells."),
        new("[GridNavProperty(sequence, cellBehavior)]", "property of a related model type", "", "Loads the navigation with the same name; the related model's [GridNestedProperty] members become columns keyed \"Nav.Property\"."),
        new("[GridNestedProperty(title)]", "property of the related model", "", "A column from the related model, with its header text."),
        new("[GridBooleanDisplayValues(trueText, falseText)]", "bool property", "True / False", "Text of a boolean cell; also what the filter matches."),
        new("[GridDateTimeFormat(format)]", "date or time property", "yyyy-MM-dd HH:mm:ss", "Format of a date or time cell (invariant culture)."),
        new("[GridAdvancedDisplay]", "property", "", "Shows the cell as a badge, coloured by GridFetchOptions.CellThemes.")
    ];

    public static readonly DocRow[] QueryMembers =
    [
        new("PageNumber", "int", "1", "The page to load, one-based. Clamp it to the available pages."),
        new("PageSize", "int", "10", "Rows per page."),
        new("SortColumnKey", "string?", "null", "The column to sort by, or null for the default order."),
        new("SortOrder", "SortOrder", "Ascending", "The sort direction."),
        new("Filters", "IReadOnlyList<MaviGridColumnFilter>", "[]", "Column filters, AND-combined: ColumnKey, Criteria, Value (invariant culture)."),
        new("KnownTotalRows", "int?", "null", "Hint: the current page's total, when only the page, size or sort changed. Use it instead of counting again."),
        new("Cursor", "MaviGridCursor?", "null", "Hint: the position after (Next) or before (Previous) the current page, to seek instead of skipping rows.")
    ];

    public static readonly DocRow[] CollectionMembers =
    [
        new("Columns", "IReadOnlyList<MaviGridColumn>", "required", "The columns in display order."),
        new("Rows", "IReadOnlyList<MaviGridRow<TKey>>", "[]", "The rows of the page: Key, Cells, ContextActions."),
        new("PageNumber, PageSize", "int", "", "The page these rows are, after clamping, and its size."),
        new("TotalRows, TotalPages", "int", "", "Totals across all pages."),
        new("SortColumnKey, SortOrder", "string?, SortOrder", "", "The sort that was applied."),
        new("AccessDenied, AccessDeniedMessage", "bool, string?", "false, null", "The fetch was denied: the grid shows the denied state."),
        new("NextPageCursor, PreviousPageCursor", "string?", "null", "Cursors for keyset paging; the grid sends them back as MaviGridQuery.Cursor."),
        new("FirstRowNumber, LastRowNumber", "int", "computed", "Row range shown in the pager.")
    ];

    public static readonly DocRow[] CallbackTypes =
    [
        new("CellClickParameters<TKey>", "record", "", "RowKey, ColumnKey, CellBehavior."),
        new("MaviContextMenuItem", "IMaviContextMenuItem", "", "Key, Title, Css, Disabled: a header menu item."),
        new("MaviRowContextMenuItem<TKey>", "IMaviRowContextMenuItem<TKey>", "", "Key (the row key), Title, Icon, Disabled, ElementVariant, ThemeColorScheme, ElementSize: a row action.")
    ];

    public static readonly DocRow[] AccessTypes =
    [
        new("IGridAccessPolicy", "interface", "", "EvaluateAsync<TDataItem>(request, ct): runs before every fetch."),
        new("GridAccessPolicy<TEntity>", "base class", "", "A policy for one entity type and its subtypes (or an interface's implementations); other types get NotApplicable."),
        new("GridAccessDecision<T>.NotApplicable() / .Allow(rowFilter?) / .Deny(message?)", "decision", "", "Not this policy's concern / allowed, optionally narrowed to some rows / denied, with a message for the user."),
        new(".WithHiddenColumns(params IEnumerable<string> columnKeys)", "decision", "", "Also hides these columns: gone from the page, and their sort and filter keys are ignored."),
        new("GridAccessRequest", "record", "", "DataItemType, ModelType, AccessTag."),
        new("GridFilterExpression.ForInterface(...)", "helper", "", "Rebinds a row filter written against an interface (ITenantScoped) to an entity, so EF Core translates it."),
        new("IGridFetchObserver", "interface", "", "OnRowsLoadedAsync (row keys, page, size, total) and OnAccessDeniedAsync, after every fetch."),
        new("IGridGlobalFilterProvider", "interface", "", "GetFilter<TDataItem>(): a row constraint for every grid, or null for types it doesn't govern.")
    ];

    public static readonly DocRow[] DataAccessTypes =
    [
        new("IGridDataFactory<TDataItem, TModel, TKey>", "service", "", "BuildPageAsync(query, options, ct): policies, filters, count, one page, rows, observers."),
        new("GridPageBuilder<TDataItem, TModel, TKey>", "static helper", "", "Build(loadedPage, query, options) and Denied(message): a page from rows your own code loaded. Columns(map, hidden) lists the columns."),
        new("GridLoadedPage<TDataItem>", "record", "", "Items, PageNumber, PageSize, TotalRows; optional NextPageCursor, PreviousPageCursor, HiddenColumns."),
        new("GridKeyset<T>", "helper", "", "LoadPageAsync(source, cursor, skip, take, load, ct): keyset paging over any IQueryable. CanSeek(type), TryCreate(keys)."),
        new("GridOrderKey", "record", "", "Path, Order, NullDefault: one key of a keyset order."),
        new("GridProjection<TDataItem>", "class", "", "Load(x => x.Member): the members a projected page loads besides the column paths."),
        new("QueryableExtensions", "extensions", "", "OrderByDynamic, ThenByDynamic, SelectPaths: order or project by a path known at run time."),
        new("IGridDataSource<T> / IGridKeysetDataSource<T>", "interfaces", "", "Count and load pages from your own store; the keyset one also seeks from cursors.")
    ];
}

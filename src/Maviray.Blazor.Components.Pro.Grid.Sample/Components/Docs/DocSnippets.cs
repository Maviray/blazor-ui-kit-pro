namespace Maviray.Blazor.Components.Pro.Grid.Sample.Components.Docs;

/// <summary>
/// The code shown on the documentation page. Kept in C# raw strings so it needs no Razor or HTML escaping; each snippet
/// matches the code running in its section.
/// </summary>
internal static class DocSnippets
{
    // 2. Getting started

    public const string Packages = """
        <!-- Your Blazor app's .csproj -->
        <ItemGroup>
          <!-- The MaviGrid component (brings the grid core and the Material components with it) -->
          <PackageReference Include="Maviray.Blazor.Components.Pro.Grid.UI" Version="*" />
          <!-- The EF Core data source; leave it out if your own code loads the pages (section 15) -->
          <PackageReference Include="Maviray.Blazor.Components.Pro.Grid.EntityFramework" Version="*" />
        </ItemGroup>
        """;

    public const string ProgramCs = """
        using Maviray.Blazor.Components.Core.Extensions;                 // AddMaviComponents
        using Maviray.Blazor.Components.Pro.Core.Extensions;              // AddMaviGrid
        using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions; // AddEntityFrameworkDataSource
        using Microsoft.EntityFrameworkCore;

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // The Material components: the grid's buttons, inputs and dialog need them.
        builder.Services.AddMaviComponents();

        // A context FACTORY, not AddDbContext: the grid opens a short-lived context for each query,
        // and several grids can fetch at the same time.
        builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connectionString));

        // The grid's data factory, reading every grid's rows through EF Core.
        builder.Services.AddMaviGrid()
            .AddEntityFrameworkDataSource<AppDbContext>();
        """;

    public const string AppRazor = """
        <head>
            ...
            <link rel="stylesheet" href="_content/Maviray.Blazor.Components.Pro.Grid.UI/css/maviray.grid.min.css" />
            <link rel="stylesheet" href="_content/Maviray.Blazor.Components.Material/css/maviray.material.min.css" />
            <link rel="stylesheet" href="https://cdn.lineicons.com/5.0/lineicons.css" />   <!-- free set: no next-page arrow; LineIcons Pro has it -->
            <script src="_content/Maviray.Blazor.Components.Material/js/maviJsInterop.js"></script>
            <HeadOutlet @rendermode="InteractiveServer" />
        </head>
        <body>
            <Routes @rendermode="InteractiveServer" />
            <script src="_framework/blazor.web.js"></script>
        </body>
        """;

    public const string Imports = """
        @using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid   @* IGridDataFactory *@
        @using Maviray.Blazor.Components.Pro.Core.Models.Grid       @* MaviGridQuery, MaviGridCollection, GridFetchOptions *@
        @using Maviray.Blazor.Components.Pro.Grid.UI.Components     @* MaviGrid, MaviGridParameters *@
        """;

    public const string MinimalModel = """
        using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
        using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
        using Maviray.Blazor.Components.Pro.Core.Interfaces;
        using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
        using System.ComponentModel.DataAnnotations;

        // One row of the grid. TDataItem is your entity (DbSampleItemOne), TKey the row key type.
        public class DocItemModel : IGridCollectionModel<DbSampleItemOne, int>
        {
            // Each property with the same name as an entity member is filled automatically,
            // and can be sorted and filtered.
            [GridDataProperty(1)]
            public int Id { get; set; }

            [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)]   // shown as a link (section 6)
            public string? Name { get; set; }

            [Display(Name = "Amount")]        // header text
            [GridDataProperty(3)]
            public int Integer { get; set; }

            [Display(Name = "Created")]
            [GridDataProperty(4)]
            [GridDateTimeFormat("yyyy-MM-dd")]
            public DateTime? Date { get; set; }

            public int Key => Id;             // the row's identity

            public void ConstructFrom(DbSampleItemOne dataItem) { }   // nothing to compute

            public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];   // no row actions
        }
        """;

    public const string MinimalPage = """
        @page "/items"
        @inject IGridDataFactory<DbSampleItemOne, DocItemModel, int> GridData

        <MaviGrid TKey="int" FetchData="FetchPageAsync" />

        @code {
            // The grid asks for one page at a time; the factory loads just that page from the database.
            private Task<MaviGridCollection<int>> FetchPageAsync(MaviGridQuery query, CancellationToken cancellationToken) =>
                GridData.BuildPageAsync(query, cancellationToken: cancellationToken);
        }
        """;

    // 3. Sorting, paging and filtering

    public const string SortingFiltering = """
        <MaviGrid TKey="int" FetchData="FetchPageAsync" Parameters="_parameters" />

        @code {
            // Create it once, as a field, not on every render.
            private readonly MaviGridParameters _parameters = new()
            {
                SortColumnKey = "Integer",          // the column key is the model property's name
                SortOrder = SortOrder.Descending,
                PageSize = 5,
                PageSizeOptions = [5, 10, 20],
                EnableFilters = true,
                FilterDebounceMilliseconds = 300,
                FilterPlaceholderText = "Filter..."
            };
        }
        """;

    // 4. Row actions

    public const string RowActionsModel = """
        // In the grid model: the actions of each row. Key is the row key the click reports.
        public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() =>
        [
            new MaviRowContextMenuItem<int>
            {
                Key = Id, Title = "Edit", Icon = "lni lni-pencil-1",
                ThemeColorScheme = ThemeColorScheme.Primary, ElementVariant = ElementVariant.Text, ElementSize = ElementSize.Small
            },
            new MaviRowContextMenuItem<int>
            {
                Key = Id, Title = "Delete", Icon = "lni lni-trash-3",
                ThemeColorScheme = ThemeColorScheme.Alert, ElementVariant = ElementVariant.Text, ElementSize = ElementSize.Small
            }
        ];
        """;

    public const string RowActionsPage = """
        <MaviGrid TKey="int" FetchData="FetchPageAsync" Parameters="_icons" OnRowContextMenuClick="OnRowAction" />
        <MaviGrid TKey="int" FetchData="FetchPageAsync" Parameters="_dropDown" OnRowContextMenuClick="OnRowAction" />

        @code {
            private readonly MaviGridParameters _icons = new() { TableRowContextMenuDisplayStyle = TableRowContextMenuDisplayStyle.Icons };
            private readonly MaviGridParameters _dropDown = new() { TableRowContextMenuDisplayStyle = TableRowContextMenuDisplayStyle.DropDown };

            private void OnRowAction(IMaviRowContextMenuItem<int> action)
            {
                // action.Key is the row's key. Check the user may do this before you do it.
                _lastAction = $"{action.Title} on row {action.Key}";
            }
        }
        """;

    // 5. Header menu

    public const string MainMenu = """
        <MaviGrid @ref="_grid" TKey="int" FetchData="FetchPageAsync"
                  ConstructMainContextMenu="BuildMenuAsync"
                  OnMainContextMenuButtonClick="OnMenuItemAsync" />

        @code {
            private MaviGrid<int>? _grid;

            // Built when the grid starts and on RefreshAsync().
            private static Task<IEnumerable<IMaviContextMenuItem>> BuildMenuAsync() =>
                Task.FromResult<IEnumerable<IMaviContextMenuItem>>(
                [
                    new MaviContextMenuItem { Key = "export", Title = "Export to CSV" },
                    new MaviContextMenuItem { Key = "refresh", Title = "Refresh" },
                    new MaviContextMenuItem { Key = "archive", Title = "Archive (not yet)", Disabled = true }
                ]);

            private async Task OnMenuItemAsync(IMaviContextMenuItem item)
            {
                if (item.Key == "refresh" && _grid is not null)
                {
                    await _grid.RefreshAsync();
                }
            }
        }
        """;

    // 6. Cell clicks

    public const string CellClicks = """
        // In the grid model: Navigational makes the column look like a link.
        [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)]
        public string? Name { get; set; }

        // In the page: cells handle clicks only when OnCellClick is set.
        <MaviGrid TKey="int" FetchData="FetchPageAsync" OnCellClick="OnCellClick" />

        @code {
            [Inject] private NavigationManager Navigation { get; set; } = default!;

            private void OnCellClick(CellClickParameters<int> cell)
            {
                if (cell.CellBehavior == MaviGridColumnCellBehavior.Navigational)
                {
                    Navigation.NavigateTo($"/items/{cell.RowKey}");
                }
            }
        }
        """;

    // 7. Selection, columns and refresh

    public const string Selection = """
        <MaviGrid @ref="_grid" TKey="int" FetchData="FetchPageAsync" Parameters="_parameters" />

        <MaviButton Title="Show selected" OnClick="ShowSelected" />
        <MaviButton Title="Clear selection" OnClick="ClearSelection" />
        <MaviButton Title="Hide / show Created" OnClick="ToggleCreated" />
        <MaviButton Title="Refresh" OnClick="RefreshAsync" />

        @code {
            private MaviGrid<int>? _grid;
            private readonly MaviGridParameters _parameters = new() { DisplaySelectColumn = true };

            private void ShowSelected(MouseClickEventArgs _) =>
                _selected = string.Join(", ", _grid!.SelectedKeys);      // kept across pages

            private void ClearSelection(MouseClickEventArgs _) => _grid!.ClearSelection();

            private void ToggleCreated(MouseClickEventArgs _) =>
                _grid!.SetColumnVisible("Date", visible: _grid.HiddenColumnKeys.Contains("Date"));

            private Task RefreshAsync(MouseClickEventArgs _) => _grid!.RefreshAsync();
        }
        """;

    // 8. Look and texts

    public const string Appearance = """
        <MaviGrid TKey="int" FetchData="FetchPageAsync" Parameters="_parameters" />

        @code {
            private readonly MaviGridParameters _parameters = new()
            {
                Striped = true,
                Condensed = true,
                MaxCellCharsToDisplay = 6,              // longer text ends in "..."; the tooltip has all of it
                RowsPerPageText = "Items per page",
                SelectNumberOfRowsToDisplayText = "How many items to show",
                GoToPreviousPageText = "Back",
                GoToNextPageText = "Forward",
                ContentMenuTitle = "More",
                NoDataAvailableTitle = "Nothing here yet"
            };
        }
        """;

    public const string ColumnWidthsModel = """
        public sealed class DocStretchModel : IGridCollectionModel<DbSampleItemOne, int>
        {
            [GridDataProperty(1)]
            public int Id { get; set; }

            [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)]
            [GridStretch(2)]                                   // twice the share of a weight-1 column
            public string? Name { get; set; }

            [Display(Name = "Amount")]
            [GridDataProperty(3)]
            public int Integer { get; set; }                   // no mark: keeps its width

            [GridNavProperty(4)]
            [GridStretch]                                      // each nested column (Details) stretches, weight 1
            public DocDetailsModel? DbSampleItemTwo { get; set; }
            ...
        }
        """;

    public const string ColumnWidthsParameters = """
        // Per grid: replaces the model's marks for this grid only. An empty dictionary stretches no column.
        private readonly MaviGridParameters _nameStretches = new() { StretchColumns = new Dictionary<string, int> { ["Name"] = 1 } };
        private readonly MaviGridParameters _weighted = new() { StretchColumns = new Dictionary<string, int> { ["Name"] = 2, ["Date"] = 1 } };

        // Shrinks to its columns instead of filling the width.
        private readonly MaviGridParameters _compact = new() { FillWidth = false };
        """;

    // 9. Badges and formatting

    public const string Formatting = """
        // The grid model
        [Display(Name = "Amount")]
        [GridDataProperty(2)]
        [GridAdvancedDisplay]                                  // a badge
        public int Integer { get; set; }

        [Display(Name = "Category")]
        [GridDataProperty(3)]
        [GridAdvancedDisplay]                                  // enum text comes from [Display] on each member
        public SampleEnum SampleEnum { get; set; }

        [Display(Name = "Status")]
        [GridDataProperty(4)]
        [GridBooleanDisplayValues("Active", "Inactive")]       // also what the filter matches
        public bool Boolean { get; set; }

        [Display(Name = "Created")]
        [GridDataProperty(5)]
        [GridDateTimeFormat("dd MMM yyyy")]
        public DateTime? Date { get; set; }
        """;

    public const string CellThemes = """
        // The page: each badge column's colour, chosen from the cell's raw value. Keyed by column key.
        private static readonly GridFetchOptions<DbSampleItemOne, DocFormattingModel> Options = new()
        {
            CellThemes = new Dictionary<string, Func<object?, ThemeColorScheme>>
            {
                ["Integer"] = value => value is int amount && amount >= 500 ? ThemeColorScheme.Success : ThemeColorScheme.Warning,
                ["SampleEnum"] = value => value is SampleEnum.One or SampleEnum.Two ? ThemeColorScheme.Primary : ThemeColorScheme.Info
            }
        };

        private Task<MaviGridCollection<int>> FetchPageAsync(MaviGridQuery query, CancellationToken cancellationToken) =>
            GridData.BuildPageAsync(query, Options, cancellationToken);
        """;

    // 10. Related data and mapped columns

    public const string RelatedData = """
        // The grid model: columns read from related entities, and columns computed from several values.
        public void ConstructFrom(OrderItem dataItem)
        {
            OrderNumber = dataItem.Order?.OrderNumber;
            CustomerCompanyName = dataItem.Order?.Customer?.CompanyName;
            CountryName = dataItem.Order?.Customer?.Region?.Country?.CountryName;
            RegionRatingOrDefault = dataItem.Order?.Customer?.Region?.Rating ?? RatingEnum.Low;
            TotalLineValue = dataItem.Quantity * dataItem.UnitPrice;   // computed: can't be sorted or filtered
        }

        // The page: load the related entities, and tell the grid where the mapped columns live,
        // so they can be sorted and filtered in the database.
        private static readonly GridFetchOptions<OrderItem, OrderItemGridModel> Options = new()
        {
            Include = query => query
                .Include(i => i.Order)
                .ThenInclude(o => o!.Customer)
                .ThenInclude(c => c!.Region)
                .ThenInclude(r => r!.Country),
            PropertyPathMap = new GridPropertyPathMap<OrderItem, OrderItemGridModel>()
                .Map(m => m.OrderNumber, "Order.OrderNumber")
                .Map(m => m.CustomerCompanyName, "Order.Customer.CompanyName")
                .Map(m => m.CountryName, "Order.Customer.Region.Country.CountryName")
                // a missing region sorts and filters as Low, the way ConstructFrom shows it
                .Map(m => m.RegionRatingOrDefault, "Order.Customer.Region.Rating", nullDefault: RatingEnum.Low)
        };
        """;

    // 11. Narrowing the rows

    public const string OptionsFilter = """
        // Only these rows, for the count and every page. AND-ed with the user's column filters.
        private static readonly GridFetchOptions<DbSampleItemOne, DocItemModel> Options = new()
        {
            Filter = item => item.Boolean && item.Integer >= 100
        };
        """;

    public const string GlobalFilter = """
        // Applies to every grid whose entity implements ISoftDeletable; other grids are untouched.
        public sealed class SoftDeleteFilter : IGridGlobalFilterProvider
        {
            public Expression<Func<TDataItem, bool>>? GetFilter<TDataItem>() where TDataItem : class =>
                typeof(ISoftDeletable).IsAssignableFrom(typeof(TDataItem))
                    ? (Expression<Func<TDataItem, bool>>)GridFilterExpression.ForInterface(
                        typeof(TDataItem), (Expression<Func<ISoftDeletable, bool>>)(x => !x.IsDeleted))
                    : null;
        }

        // Program.cs
        builder.Services.AddMaviGrid()
            .AddEntityFrameworkDataSource<AppDbContext>()
            .AddGlobalFilter<SoftDeleteFilter>();
        """;

    // 12. Empty, error, denied and prerender states

    public const string NoData = """
        <MaviGrid TKey="int" FetchData="FetchPageAsync">
            <NoDataContent>
                <span>No items match. <a href="/items/new">Add one</a></span>
            </NoDataContent>
        </MaviGrid>
        """;

    public const string FetchError = """
        <MaviGrid TKey="int" FetchData="FetchPageAsync" OnFetchError="OnFetchError">
            <FetchErrorContent>
                <span>The items service is down. Try again in a minute.</span>
            </FetchErrorContent>
        </MaviGrid>

        @code {
            // FetchData threw. (The grid also logs it.)
            private void OnFetchError(Exception exception) => _lastError = exception.Message;
        }
        """;

    public const string Denied = """
        // An access policy denied the fetch (section 13): the grid keeps its headers and shows the
        // policy's message, or MaviGridParameters.AccessDeniedTitle when it gives none.
        GridAccessDecision<DbSampleItemOne>.Deny("Only managers can see these items.");

        // Or your own message:
        <MaviGrid TKey="int" FetchData="FetchPageAsync">
            <AccessDeniedContent><span>Ask your manager for access.</span></AccessDeniedContent>
        </MaviGrid>
        """;

    public const string Prerender = """
        <MaviGrid TKey="int" FetchData="FetchPageAsync" Parameters="_parameters">
            <PrerenderContent><span>Loading items...</span></PrerenderContent>
        </MaviGrid>

        @code {
            // Don't query while prerendering: show PrerenderContent, then fetch once the page is interactive.
            // (Or keep the prerendered rows with PrerenderedPageKey, section 14.)
            private readonly MaviGridParameters _parameters = new() { FetchWhilePrerendering = false };
        }
        """;

    // 13. Access policies, hidden columns and audit

    public const string AccessPolicy = """
        // Decides before every fetch of a DbSampleItemOne grid (or a subtype) tagged "docs-access".
        public sealed class DocsAccessPolicy(DocsAccessState state) : GridAccessPolicy<DbSampleItemOne>
        {
            public const string AccessTag = "docs-access";

            protected override ValueTask<GridAccessDecision<DbSampleItemOne>> EvaluateAsync(
                GridAccessRequest request, CancellationToken cancellationToken)
            {
                if (request.AccessTag != AccessTag)
                {
                    return ValueTask.FromResult(GridAccessDecision<DbSampleItemOne>.NotApplicable());
                }

                var decision = state.OnlySmallAmounts
                    ? GridAccessDecision<DbSampleItemOne>.Allow(item => item.Integer < 500)   // only these rows
                    : GridAccessDecision<DbSampleItemOne>.Allow();

                // Hidden columns are left out of the page, and can't be sorted or filtered on.
                return ValueTask.FromResult(state.HideCreatedColumn ? decision.WithHiddenColumns("Date") : decision);
            }
        }
        """;

    public const string FetchObserver = """
        // Told what every fetch loaded: the row keys in display order, the page and the total.
        public sealed class DocsAuditObserver(DocsAuditLog log) : IGridFetchObserver
        {
            public ValueTask OnRowsLoadedAsync<TDataItem, TKey>(GridRowsLoaded<TKey> loaded, CancellationToken cancellationToken)
                where TDataItem : class
                where TKey : IEquatable<TKey>
            {
                if (loaded.Request.AccessTag == DocsAccessPolicy.AccessTag)
                {
                    log.Add($"page {loaded.PageNumber}: keys {string.Join(", ", loaded.RowKeys)} of {loaded.TotalRows}");
                }

                return ValueTask.CompletedTask;
            }

            // OnAccessDeniedAsync has a default (do nothing); override it to record denials.
        }
        """;

    public const string AccessRegistration = """
        // Program.cs
        builder.Services.AddMaviGrid(options => options.RequireExplicitAllow = false)   // true: deny what no policy allows
            .AddEntityFrameworkDataSource<AppDbContext>()
            .AddAccessPolicy<DocsAccessPolicy>()
            .AddFetchObserver<DocsAuditObserver>();

        // The page: the tag tells the policy and the observer which grid this is.
        private static readonly GridFetchOptions<DbSampleItemOne, DocItemModel> Options = new()
        {
            AccessTag = DocsAccessPolicy.AccessTag
        };
        """;

    // 14. Fast paging

    public const string FastPaging = """
        <MaviGrid TKey="int" FetchData="FetchPageAsync" PrerenderedPageKey="docs-fast-paging" />

        @code {
            private static readonly GridFetchOptions<DbSampleItemOne, DocItemModel> Options = new()
            {
                // Select only the shown columns (plus any member ConstructFrom reads: .Load(x => x.Member)).
                Projection = new GridProjection<DbSampleItemOne>()
            };

            // Count reuse and keyset paging need nothing here: the grid sends the hints,
            // the factory and the EF data source use them.
            private Task<MaviGridCollection<int>> FetchPageAsync(MaviGridQuery query, CancellationToken cancellationToken) =>
                GridData.BuildPageAsync(query, Options, cancellationToken);
        }

        // Program.cs (these are the defaults)
        builder.Services.AddMaviGrid(options =>
        {
            options.ReuseTotalRows = true;   // skip the count when only the page, size or sort changed
            options.MaxPageSize = 100;
        });
        """;

    // 15. Your own service

    public const string OwnServicePage = """
        @inject ProductService Products

        <MaviGrid TKey="int" FetchData="FetchPageAsync" Parameters="_parameters" />

        @code {
            private async Task<MaviGridCollection<int>> FetchPageAsync(MaviGridQuery query, CancellationToken cancellationToken)
            {
                // Only DTO-backed columns are sortable and filterable, so these are the only keys the grid sends.
                var nameFilter = query.Filters.FirstOrDefault(f => f.ColumnKey == "Name")?.Value;
                var request = new ProductPageRequest(query.PageNumber, query.PageSize, query.SortColumnKey ?? "Id",
                    query.SortOrder, nameFilter, query.KnownTotalRows, query.Cursor);

                var page = await Products.GetPageAsync(request, cancellationToken);   // your authorization runs here

                // Columns, cells and formatting from ProductRow's attributes, as the factory would build them.
                return GridPageBuilder<ProductDto, ProductRow, int>.Build(
                    new GridLoadedPage<ProductDto>(page.Items, page.Page, page.PageSize, page.TotalCount)
                    {
                        NextPageCursor = page.NextCursor,
                        PreviousPageCursor = page.PreviousCursor
                    },
                    query);
            }
        }
        """;

    public const string OwnServiceCode = """
        // Inside the service: use the grid's hints with your own query.
        var total = request.KnownTotal ?? rows.Count();          // skip the count the grid already has

        // Sort, then the key to break ties. With a cursor it seeks (WHERE ... > ...) instead of skipping rows.
        var loaded = await new GridKeyset<ProductDto>(
            [new GridOrderKey(request.SortBy, request.SortOrder), new GridOrderKey("Id", request.SortOrder)])
            .LoadPageAsync(rows, request.Cursor, (page - 1) * request.PageSize, request.PageSize,
                (query, ct) => query.ToListAsync(ct), cancellationToken);

        // loaded.Items, loaded.FirstRowCursor, loaded.LastRowCursor

        // A denial from your service: the grid shows its access-denied state.
        return GridPageBuilder<ProductDto, ProductRow, int>.Denied("You can't view products.");
        """;

    // 16. The grid model

    public const string DataModel = """
        // One row of the grid. DbSampleItemOne is the entity the factory loads; int is the row key type.
        public sealed class DocFormattingModel : IGridCollectionModel<DbSampleItemOne, int>
        {
            // [GridDataProperty(sequence)] makes a column; columns are ordered by sequence.
            // A property named like an entity member is copied from it, and can be sorted and filtered.
            [GridDataProperty(1)]
            public int Id { get; set; }

            [Display(Name = "Amount")]                        // header text
            [GridDataProperty(2)]
            [GridAdvancedDisplay]                             // badge, coloured by GridFetchOptions.CellThemes
            public int Integer { get; set; }

            [Display(Name = "Category")]
            [GridDataProperty(3)]
            [GridAdvancedDisplay]
            public SampleEnum SampleEnum { get; set; }        // enum text from [Display] on each member

            [Display(Name = "Status")]
            [GridDataProperty(4)]
            [GridBooleanDisplayValues("Active", "Inactive")]
            public bool Boolean { get; set; }

            [Display(Name = "Created")]
            [GridDataProperty(5)]
            [GridDateTimeFormat("dd MMM yyyy")]
            public DateTime? Date { get; set; }

            [Display(Name = "Price")]
            [GridDataProperty(6)]
            public decimal Decimal { get; set; }

            // Computed in ConstructFrom. No entity member has this name, so it can't be sorted or filtered
            // (unless a GridPropertyPathMap gives it a path).
            [Display(Name = "Price incl. VAT")]
            [GridDataProperty(7)]
            public decimal PriceWithVat { get; set; }

            // A related entity, named like the entity's navigation: it is loaded for the page, and its
            // [GridNestedProperty] members become columns keyed "DbSampleItemTwo.Description".
            [GridNavProperty(8)]
            public DocDetailsModel? DbSampleItemTwo { get; set; }

            public int Key => Id;

            // Runs after the same-named members are copied.
            public void ConstructFrom(DbSampleItemOne dataItem) => PriceWithVat = Math.Round(dataItem.Decimal * 1.2m, 2);

            public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
        }

        public sealed class DocDetailsModel
        {
            [GridNestedProperty("Details")]
            public string? Description { get; set; }
        }
        """;
}

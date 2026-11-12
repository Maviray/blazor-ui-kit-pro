using Maviray.Blazor.Components.Core.Constants;
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Core.EventArgs;
using Maviray.Blazor.Components.Core.Extensions;
using Maviray.Blazor.Components.Material.Components.Feedback;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Globalization;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Components;

/// <summary>
/// A server-paged data grid. Every page, sort or filter change asks <see cref="FetchData"/> for exactly the rows to
/// show; the grid keeps only the current page.
/// </summary>
/// <remarks>
/// Rendering is explicit: events don't re-render the whole grid unless they change what it shows (typing in a filter
/// doesn't), and rows re-render only when their data, selection, menu or the layout changes.
/// </remarks>
public partial class MaviGrid<TKey> : IHandleEvent, IAsyncDisposable where TKey : IEquatable<TKey>
{
    private static readonly MaviDialogBaseParameters ColumnsDialogParameters = new()
    {
        ConfirmButtonTitle = "Set",
        Title = "Set column visibility",
        Width = "w-96"
    };

    private readonly Dictionary<string, string> _filterText = new(StringComparer.Ordinal);
    private readonly HashSet<string> _hiddenColumns = new(StringComparer.Ordinal);
    private readonly HashSet<TKey> _selectedKeys = [];

    private int _pageNumber = 1;
    private int _pageSize;
    private string? _sortColumnKey;
    private SortOrder _sortOrder;

    private MaviGridCollection<TKey>? _page;
    private MaviGridQuery? _pageQuery;
    private MaviGridQuery? _appliedQuery;
    private PersistingComponentStateSubscription? _persisting;
    private IReadOnlyList<MaviGridColumn> _knownColumns = [];
    private IReadOnlyList<MaviGridColumn> _filterColumns = [];
    private Exception? _fetchError;
    private bool _busy = true;
    private bool _prerenderSkipped;
    private CancellationTokenSource? _fetchCts;
    private CancellationTokenSource? _filterCts;

    private IReadOnlyList<IMaviContextMenuItem> _mainMenu = [];
    private bool _mainMenuStale;
    private bool _mainMenuOpen;
    private int _openRowMenu = -1;
    private string _rowMenuStyle = string.Empty;

    private MaviDialog? _columnsDialog;
    private ColumnChoice[]? _columnChoices;
    private bool _displayColumnsDialog;

    private MaviGridLayout _layout = MaviGridLayout.Empty;
    private bool _layoutDirty = true;
    private int _layoutVersion;
    private (bool, bool, bool, bool, TableRowContextMenuDisplayStyle, int) _appliedSettings;

    private DotNetObjectReference<MaviGrid<TKey>>? _dotNetRef;
    private bool _disposed;

    /// <summary>Loads one page. Pass the token on to the data factory so superseded fetches stop early.</summary>
    [Parameter, EditorRequired]
    public Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<TKey>>> FetchData { get; set; } = default!;

    [Parameter]
    public MaviGridParameters Parameters { get; set; } = new();

    /// <summary>Builds the grid's header menu; called when the grid starts and on <see cref="RefreshAsync"/>.</summary>
    [Parameter]
    public Func<Task<IEnumerable<IMaviContextMenuItem>>>? ConstructMainContextMenu { get; set; }

    [Parameter]
    public EventCallback<IMaviContextMenuItem> OnMainContextMenuButtonClick { get; set; }

    [Parameter]
    public EventCallback<IMaviRowContextMenuItem<TKey>> OnRowContextMenuClick { get; set; }

    [Parameter]
    public EventCallback<CellClickParameters<TKey>> OnCellClick { get; set; }

    /// <summary>Raised when a fetch fails (not when the grid cancels a fetch it no longer needs).</summary>
    [Parameter]
    public EventCallback<Exception> OnFetchError { get; set; }

    [Parameter]
    public RenderFragment? NoDataContent { get; set; }

    [Parameter]
    public RenderFragment? AccessDeniedContent { get; set; }

    [Parameter]
    public RenderFragment? FetchErrorContent { get; set; }

    /// <summary>Shown while prerendering when <see cref="MaviGridParameters.FetchWhilePrerendering"/> is false.</summary>
    [Parameter]
    public RenderFragment? PrerenderContent { get; set; }

    /// <summary>
    /// When set, the page fetched while prerendering is stored under this key (unique among the grids on the page)
    /// with <c>PersistentComponentState</c>, and the interactive grid starts from it instead of fetching it again: one
    /// query and one audit entry per page view, with the rows shown at once.
    /// </summary>
    /// <remarks>
    /// The page is reused only for the same initial query (page, size, sort). Its row actions come back as
    /// <see cref="MaviRowContextMenuItem{TKey}"/> with the same values. Like the prerendered HTML, the stored page
    /// reaches the browser, so it holds nothing the user couldn't already see.
    /// </remarks>
    [Parameter]
    public string? PrerenderedPageKey { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    /// <summary>Keys of the selected rows, kept across pages.</summary>
    public IReadOnlyCollection<TKey> SelectedKeys => _selectedKeys;

    /// <summary>Keys of the columns the user (or the app) has hidden.</summary>
    public IReadOnlyCollection<string> HiddenColumnKeys => _hiddenColumns;

    internal bool HandlesCellClicks => OnCellClick.HasDelegate;

    /// <summary>The layout rows render with; <see cref="LayoutVersion"/> changes whenever it is rebuilt.</summary>
    internal MaviGridLayout CurrentLayout => Layout;

    internal int LayoutVersion => _layoutVersion;

    private string MainMenuId => $"{Id}-menu";

    private MaviGridLayout Layout
    {
        get
        {
            if (_layoutDirty)
            {
                _layout = MaviGridLayout.Create(_knownColumns, _hiddenColumns, Parameters, _page?.AccessDenied == true);
                _layoutDirty = false;
                _layoutVersion++;
            }

            return _layout;
        }
    }

    /// <summary>Fetches the current page again, counting the rows afresh, and rebuilds the header menu.</summary>
    public Task RefreshAsync() => FetchAsync(rebuildMainMenu: true, recount: true);

    public void ClearSelection()
    {
        _selectedKeys.Clear();
        StateHasChanged();
    }

    public void SetColumnVisible(string columnKey, bool visible)
    {
        if (visible ? _hiddenColumns.Remove(columnKey) : _hiddenColumns.Add(columnKey))
        {
            _layoutDirty = true;
            StateHasChanged();
        }
    }

    // Events re-render only when a handler asks for it (StateHasChanged), so typing in a filter or hovering costs nothing.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Parameters ??= new MaviGridParameters();
        _sortColumnKey = Parameters.SortColumnKey;
        _sortOrder = Parameters.SortOrder;
        _pageSize = Parameters.PageSize;
        ApplySettings();
    }

    protected override void OnParametersSet()
    {
        Parameters ??= new MaviGridParameters();
        ApplySettings();
    }

    protected override async Task OnInitializedAsync()
    {
        if (TryRestorePrerenderedPage())
        {
            await RebuildMainMenuIfStaleAsync();
            return;
        }

        if (!Parameters.FetchWhilePrerendering && !RendererInfo.IsInteractive)
        {
            _prerenderSkipped = true;
            _busy = false;
            return;
        }

        await FetchAsync(rebuildMainMenu: true);
    }

    /// <summary>
    /// While prerendering, arranges for the fetched page to be stored; once interactive, starts from the stored page
    /// when it answers the initial query. Returns whether it did.
    /// </summary>
    private bool TryRestorePrerenderedPage()
    {
        if (PrerenderedPageKey is not { } key || ServiceProvider.GetService(typeof(PersistentComponentState)) is not PersistentComponentState state)
        {
            return false;
        }

        if (!RendererInfo.IsInteractive)
        {
            _persisting = state.RegisterOnPersisting(() =>
            {
                // A failed fetch leaves no page: the interactive grid fetches again.
                if (_page is not null && _appliedQuery is not null)
                {
                    state.PersistAsJson(key, MaviGridPersistedPage<TKey>.From(_page, _appliedQuery));
                }

                return Task.CompletedTask;
            });

            return false;
        }

        if (!state.TryTakeFromJson<MaviGridPersistedPage<TKey>>(key, out var persisted) || persisted is null || !persisted.Answers(BuildQuery(recount: false)))
        {
            return false;
        }

        ApplyPage(persisted.ToCollection(), persisted.Query);
        _busy = false;
        _mainMenuStale = true;
        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync(JsInteropConstants.REGISTER_OUT_OF_FOCUS_CALLBACK_LISTENER, Id, _dotNetRef, nameof(HandleOutsideClick));
        }

        // The dialog is mounted by the render that just finished; show it now that its reference is set.
        if (_displayColumnsDialog && _columnsDialog is not null)
        {
            _displayColumnsDialog = false;
            await _columnsDialog.Display();
        }
    }

    private void ApplySettings()
    {
        var settings = (Parameters.Condensed, Parameters.Striped, Parameters.DisplaySelectColumn, Parameters.EnableFilters,
            Parameters.TableRowContextMenuDisplayStyle, Parameters.MaxCellCharsToDisplay);

        if (settings != _appliedSettings)
        {
            _appliedSettings = settings;
            _layoutDirty = true;
        }
    }

    /// <param name="rebuildMainMenu">Rebuild the header menu once a fetch completes.</param>
    /// <param name="recount">Send no known total, so the fetch code counts the rows again.</param>
    private async Task FetchAsync(bool rebuildMainMenu = false, bool recount = false)
    {
        // A filter pause can end just before disposal; a grid no one sees mustn't query (or be audited).
        if (_disposed)
        {
            return;
        }

        // Kept until a fetch completes, so a newer fetch that supersedes a refresh still rebuilds the menu.
        _mainMenuStale |= rebuildMainMenu;

        var cts = new CancellationTokenSource();
        var superseded = _fetchCts;
        _fetchCts = cts;
        superseded?.Cancel();

        var query = BuildQuery(recount);
        _busy = true;

        if (HasRendered)
        {
            StateHasChanged();
        }

        try
        {
            var page = await FetchData(query, cts.Token);

            if (cts.IsCancellationRequested)
            {
                return;
            }

            ApplyPage(page, query);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // The grid cancelled this fetch: a newer one started (and finishes the work) or the grid was disposed.
            return;
        }
        catch (Exception ex) when (!cts.IsCancellationRequested)
        {
            Logger?.LogError(ex, "Grid fetch failed.");
            _page = null;
            _pageQuery = null;
            _appliedQuery = null;
            _fetchError = ex;
            _layoutDirty = true;

            if (OnFetchError.HasDelegate)
            {
                await OnFetchError.InvokeAsync(ex);
            }
        }
        catch (Exception ex)
        {
            Logger?.LogDebug(ex, "A superseded grid fetch failed; its result is discarded.");
        }
        finally
        {
            if (ReferenceEquals(_fetchCts, cts))
            {
                _fetchCts = null;
                _busy = false;

                if (!_disposed)
                {
                    StateHasChanged();
                }
            }

            cts.Dispose();
        }

        await RebuildMainMenuIfStaleAsync();
    }

    // The menu is the app's own callback: its failures are the app's, not a fetch error.
    private async Task RebuildMainMenuIfStaleAsync()
    {
        if (_mainMenuStale && ConstructMainContextMenu is not null && !_disposed)
        {
            _mainMenuStale = false;
            _mainMenu = [.. await ConstructMainContextMenu()];
            StateHasChanged();
        }
    }

    private void ApplyPage(MaviGridCollection<TKey> page, MaviGridQuery query)
    {
        if (!ReferenceEquals(_knownColumns, page.Columns) || _page?.AccessDenied != page.AccessDenied)
        {
            _layoutDirty = true;
        }

        _page = page;
        _appliedQuery = query;
        _pageQuery = page.AccessDenied ? null : query;
        _knownColumns = page.Columns;

        // A denied page's columns aren't filterable; keep resolving typed filters against the last allowed columns.
        if (!page.AccessDenied || _filterColumns.Count == 0)
        {
            _filterColumns = page.Columns;
        }
        _fetchError = null;
        _openRowMenu = -1;

        // Adopt what the server applied (a clamped page number, a page size capped by MaxPageSize).
        if (!page.AccessDenied)
        {
            _pageNumber = page.PageNumber;

            if (page.PageSize > 0)
            {
                _pageSize = page.PageSize;
            }
        }
    }

    private MaviGridQuery BuildQuery(bool recount)
    {
        var filters = ResolveFilters();

        // The count depends only on the filters: a page, page size or sort change keeps it.
        var sameRows = !recount && _pageQuery is not null && _page is not null && filters.SequenceEqual(_pageQuery.Filters);

        // A cursor is a position in one order of one row set; page 1 is as fast to skip to as to seek.
        var sameOrder = sameRows && _pageQuery!.PageSize == _pageSize && _pageQuery.SortColumnKey == _sortColumnKey && _pageQuery.SortOrder == _sortOrder;

        MaviGridCursor? cursor = null;

        if (sameOrder && _pageNumber > 1)
        {
            if (_pageNumber == _page!.PageNumber + 1 && _page.NextPageCursor is { } next)
            {
                cursor = new MaviGridCursor(next, MaviGridCursorDirection.Next);
            }
            else if (_pageNumber == _page.PageNumber - 1 && _page.PreviousPageCursor is { } previous)
            {
                cursor = new MaviGridCursor(previous, MaviGridCursorDirection.Previous);
            }
        }

        return new MaviGridQuery
        {
            PageNumber = _pageNumber,
            PageSize = _pageSize,
            SortColumnKey = _sortColumnKey,
            SortOrder = _sortOrder,
            Filters = filters,
            KnownTotalRows = sameRows ? _page!.TotalRows : null,
            Cursor = cursor
        };
    }

    private MaviGridColumnFilter[] ResolveFilters()
    {
        if (_filterText.Count == 0)
        {
            return [];
        }

        var filters = new List<MaviGridColumnFilter>(_filterText.Count);

        foreach (var column in _filterColumns)
        {
            if (column.Filterable && _filterText.TryGetValue(column.Key, out var text) && TryResolveFilter(column, text) is { } filter)
            {
                filters.Add(filter);
            }
        }

        return [.. filters];
    }

    /// <summary>
    /// Reads typed filter text for the column's type: strings match by substring, numbers, dates and ids by equality,
    /// booleans and enums by their display text. Returns <see langword="null"/> for text the column can't use.
    /// </summary>
    private static MaviGridColumnFilter? TryResolveFilter(MaviGridColumn column, string text)
    {
        var value = text.Trim();
        var invariant = CultureInfo.InvariantCulture;

        MaviGridColumnFilter Equal(string v) => new(column.Key, MaviGridFilterCriteria.Equals, v);

        return column.PropertyType switch
        {
            MaviGridPropertyType.String => new MaviGridColumnFilter(column.Key, MaviGridFilterCriteria.Contains, value),
            MaviGridPropertyType.Char => value.Length == 1 ? Equal(value) : null,
            MaviGridPropertyType.Int => long.TryParse(value, NumberStyles.Integer, invariant, out _) ? Equal(value) : null,
            MaviGridPropertyType.Decimal => decimal.TryParse(value, NumberStyles.Number, invariant, out _) ? Equal(value) : null,
            MaviGridPropertyType.DateTime => DateTime.TryParse(value, invariant, DateTimeStyles.None, out _) ? Equal(value) : null,
            MaviGridPropertyType.TimeSpan => TimeSpan.TryParse(value, invariant, out _) ? Equal(value) : null,
            MaviGridPropertyType.Guid => Guid.TryParse(value, out _) ? Equal(value) : null,
            MaviGridPropertyType.Bool => Matches(column.TrueText, value) ? new MaviGridColumnFilter(column.Key, MaviGridFilterCriteria.IsTrue, bool.TrueString)
                : Matches(column.FalseText, value) ? new MaviGridColumnFilter(column.Key, MaviGridFilterCriteria.IsFalse, bool.FalseString)
                : null,
            MaviGridPropertyType.Enum => column.EnumOptions.FirstOrDefault(o => Matches(o.Text, value)) is { Name: not null } option ? Equal(option.Name) : null,
            _ => Equal(value)
        };

        static bool Matches(string label, string typed) => label.Contains(typed, StringComparison.OrdinalIgnoreCase);
    }

    private async Task SortAsync(MaviGridColumn column)
    {
        if (!column.Sortable)
        {
            return;
        }

        if (string.Equals(_sortColumnKey, column.Key, StringComparison.Ordinal))
        {
            _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }
        else
        {
            _sortColumnKey = column.Key;
            _sortOrder = SortOrder.Ascending;
        }

        _pageNumber = 1;
        await FetchAsync();
    }

    private Task PreviousPageAsync() => GoToPageAsync(_pageNumber - 1);

    private Task NextPageAsync() => GoToPageAsync(_pageNumber + 1);

    private Task GoToPageAsync(int pageNumber)
    {
        var lastPage = Math.Max(_page?.TotalPages ?? 1, 1);
        var target = Math.Clamp(pageNumber, 1, lastPage);

        if (target == _pageNumber && _page is not null && _page.PageNumber == target)
        {
            // Nothing to load, but re-render so a clamped value replaces what was typed.
            StateHasChanged();
            return Task.CompletedTask;
        }

        _pageNumber = target;
        return FetchAsync();
    }

    private Task OnGoToPageAsync(ChangeEventArgs e) =>
        int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var page) ? GoToPageAsync(page) : Task.CompletedTask;

    private Task OnPageSizeChangedAsync(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) || size == _pageSize)
        {
            return Task.CompletedTask;
        }

        _pageSize = size;
        _pageNumber = 1;
        return FetchAsync();
    }

    /// <summary>The offered page sizes, plus the current one if the server applied a size that isn't offered.</summary>
    private IEnumerable<int> PageSizeChoices() =>
        Parameters.PageSizeOptions.Contains(_pageSize) ? Parameters.PageSizeOptions : Parameters.PageSizeOptions.Append(_pageSize).Order();

    private string FilterText(string columnKey) => _filterText.GetValueOrDefault(columnKey, string.Empty);

    private void OnFilterInput(string columnKey, ChangeEventArgs e)
    {
        var text = e.Value?.ToString();

        if (string.IsNullOrWhiteSpace(text))
        {
            _filterText.Remove(columnKey);
        }
        else
        {
            _filterText[columnKey] = text;
        }

        _filterCts?.Cancel();
        _filterCts?.Dispose();
        _filterCts = new CancellationTokenSource();
        _ = ApplyFiltersAfterPauseAsync(_filterCts.Token);
    }

    private async Task ApplyFiltersAfterPauseAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (Parameters.FilterDebounceMilliseconds > 0)
            {
                await Task.Delay(Parameters.FilterDebounceMilliseconds, cancellationToken);
            }

            await InvokeAsync(() =>
            {
                _pageNumber = 1;
                return FetchAsync();
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A newer keystroke restarted the pause.
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private Task ClearFiltersAndReloadAsync()
    {
        _filterText.Clear();
        _pageNumber = 1;
        return FetchAsync(recount: true);
    }

    private void ToggleMainMenu()
    {
        _mainMenuOpen = !_mainMenuOpen;
        StateHasChanged();
    }

    private async Task OnMainMenuItemAsync(IMaviContextMenuItem item)
    {
        _mainMenuOpen = false;
        StateHasChanged();

        if (OnMainContextMenuButtonClick.HasDelegate)
        {
            await OnMainContextMenuButtonClick.InvokeAsync(item);
        }
    }

    internal string RowMenuId(int rowIndex) => $"{Id}-row-menu-{rowIndex}";

    internal string DisplayText(string text) =>
        Parameters.MaxCellCharsToDisplay > 0 ? text.TrimToLengthWithDots(Parameters.MaxCellCharsToDisplay) : text;

    internal void ToggleRowMenu(int rowIndex, MouseClickEventArgs args)
    {
        _openRowMenu = _openRowMenu == rowIndex ? -1 : rowIndex;
        _rowMenuStyle = $"top:{(args.ClientY + 8).ToString(CultureInfo.InvariantCulture)}px;left:{(args.ClientX - 248).ToString(CultureInfo.InvariantCulture)}px;";
        StateHasChanged();
    }

    internal async Task OnRowActionAsync(IMaviRowContextMenuItem<TKey> action)
    {
        if (_openRowMenu >= 0)
        {
            _openRowMenu = -1;
            StateHasChanged();
        }

        if (OnRowContextMenuClick.HasDelegate)
        {
            await OnRowContextMenuClick.InvokeAsync(action);
        }
    }

    internal Task OnCellClickAsync(MaviGridRow<TKey> row, MaviGridColumn column) =>
        OnCellClick.InvokeAsync(new CellClickParameters<TKey>(row.Key, column.Key, column.CellBehavior));

    internal void SetSelected(TKey key, bool selected)
    {
        if (selected ? _selectedKeys.Add(key) : _selectedKeys.Remove(key))
        {
            StateHasChanged();
        }
    }

    /// <summary>Called by the outside-click script with the menu that was clicked, if any; closes every other open menu.</summary>
    [JSInvokable]
    public void HandleOutsideClick(string clickedMenuId)
    {
        var changed = false;

        if (_mainMenuOpen && clickedMenuId != MainMenuId)
        {
            _mainMenuOpen = false;
            changed = true;
        }

        if (_openRowMenu >= 0 && clickedMenuId != RowMenuId(_openRowMenu))
        {
            _openRowMenu = -1;
            changed = true;
        }

        if (changed)
        {
            StateHasChanged();
        }
    }

    private void OpenColumnsDialog()
    {
        _columnChoices = _knownColumns.Select(c => new ColumnChoice(c.Key, c.Title) { Visible = !_hiddenColumns.Contains(c.Key) }).ToArray();
        _displayColumnsDialog = true;
        StateHasChanged();
    }

    private async Task OnColumnsDialogButtonAsync(DialogButtonClickEventArgs args)
    {
        if (args.ButtonClicked == DialogButtonClick.Confirm && _columnChoices is not null)
        {
            _hiddenColumns.Clear();
            _hiddenColumns.UnionWith(_columnChoices.Where(c => !c.Visible).Select(c => c.Key));
            _layoutDirty = true;
        }

        if (_columnsDialog is not null)
        {
            await _columnsDialog.Hide();
        }

        _columnChoices = null;
        _columnsDialog = null;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _persisting?.Dispose();
        _fetchCts?.Cancel();
        _filterCts?.Cancel();
        _filterCts?.Dispose();

        if (_dotNetRef is not null)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync(JsInteropConstants.UN_REGISTER_OUT_OF_FOCUS_CALLBACK_LISTENER, Id);
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone; so is the listener.
            }

            _dotNetRef.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>One checkbox in the column visibility dialog.</summary>
    private sealed class ColumnChoice(string key, string title)
    {
        public string Key { get; } = key;

        public string Title { get; } = title;

        public bool Visible { get; set; }
    }
}

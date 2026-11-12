using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>
/// Builds grid pages. Per fetch: evaluate access policies before any data source call; combine column filters, the
/// grid's filter, global filters and policy row filters into one predicate; count; load only the requested page
/// (skipped when nothing matches); map and format the rows; notify fetch observers.
/// </summary>
public sealed class GridDataFactory<TDataItem, TModel, TKey> : IGridDataFactory<TDataItem, TModel, TKey>
    where TDataItem : class
    where TModel : class, IGridCollectionModel<TDataItem, TKey>
    where TKey : IEquatable<TKey>
{
    private static readonly string DataItemName = typeof(TDataItem).Name;

    private readonly IGridDataSource<TDataItem> _dataSource;
    private readonly IGridGlobalFilterProvider[] _globalFilters;
    private readonly IGridAccessPolicy[] _policies;
    private readonly IGridFetchObserver[] _observers;
    private readonly GridDataFactoryOptions _options;
    private readonly ILogger _logger;

    public GridDataFactory(
        IGridDataSource<TDataItem> dataSource,
        IEnumerable<IGridGlobalFilterProvider> globalFilters,
        IEnumerable<IGridAccessPolicy> accessPolicies,
        IEnumerable<IGridFetchObserver> fetchObservers,
        IOptions<GridDataFactoryOptions> options,
        ILogger<GridDataFactory<TDataItem, TModel, TKey>> logger)
    {
        _dataSource = dataSource;
        _globalFilters = globalFilters.ToArray();
        _policies = accessPolicies.ToArray();
        _observers = fetchObservers.ToArray();
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MaviGridCollection<TKey>> BuildPageAsync(
        MaviGridQuery query,
        GridFetchOptions<TDataItem, TModel>? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var metadata = GridModelMetadata<TDataItem, TModel>.Instance;
        var request = new GridAccessRequest(typeof(TDataItem), typeof(TModel), options?.AccessTag);
        var pageSize = Math.Clamp(query.PageSize, 1, _options.MaxPageSize);
        var access = await EvaluateAccessAsync(request, cancellationToken).ConfigureAwait(false);

        if (access.Denied)
        {
            return await DenyAsync(request, access.DeniedMessage, metadata, pageSize, cancellationToken).ConfigureAwait(false);
        }

        // Hidden columns are gone from here on: their keys can't sort, filter or be formatted.
        var columns = metadata.ColumnsFor(options?.PropertyPathMap).Without(access.HiddenColumns);
        var sort = ResolveSort(query, columns);

        // A projection loads the visible columns' paths and the members the model reads, so no navigation is included whole.
        var selectPaths = options?.Projection is { } projection ? columns.Paths.Select(p => p.DataItemPath).Concat(projection.Paths).Distinct(StringComparer.Ordinal).ToArray() : null;

        var dataSourceQuery = new GridDataSourceQuery<TDataItem>
        {
            Filter = CombineFilters(query, options, columns, access.RowFilters),
            Sort = sort,
            IncludePaths = selectPaths is null ? metadata.IncludePaths : [],
            SelectPaths = selectPaths,
            Include = options?.Include
        };

        // A zero total is always counted: nothing would be loaded for it, so added rows could never show up.
        var knownTotal = _options.ReuseTotalRows && query.KnownTotalRows > 0 ? query.KnownTotalRows : null;
        var totalRows = knownTotal ?? await _dataSource.CountAsync(dataSourceQuery, cancellationToken).ConfigureAwait(false);
        var pageNumber = ClampPage(query.PageNumber, totalRows, pageSize);

        // The cursor leads to the requested page; once that page is clamped away, only skipping gets there.
        var cursor = pageNumber == query.PageNumber ? query.Cursor : null;
        var loaded = await LoadPageAsync(dataSourceQuery, cursor, pageNumber, pageSize, totalRows, cancellationToken).ConfigureAwait(false);

        if (knownTotal is not null && loaded.Items.Count != RowsOnPage(totalRows, pageNumber, pageSize))
        {
            // Rows were added or removed since the grid's count: count again, and move to the last page if this one is gone.
            GridLog.TotalRowsStale(_logger, DataItemName, knownTotal.Value);
            totalRows = await _dataSource.CountAsync(dataSourceQuery, cancellationToken).ConfigureAwait(false);
            var clamped = ClampPage(query.PageNumber, totalRows, pageSize);

            if (clamped != pageNumber)
            {
                pageNumber = clamped;
                loaded = await LoadPageAsync(dataSourceQuery, cursor: null, pageNumber, pageSize, totalRows, cancellationToken).ConfigureAwait(false);
            }
        }

        var totalPages = TotalPages(totalRows, pageSize);
        var page = GridPageBuilder<TDataItem, TModel, TKey>.Build(
            new GridLoadedPage<TDataItem>(loaded.Items, pageNumber, pageSize, totalRows)
            {
                NextPageCursor = pageNumber < totalPages ? loaded.LastRowCursor : null,
                PreviousPageCursor = pageNumber > 1 ? loaded.FirstRowCursor : null,
                HiddenColumns = access.HiddenColumns
            },
            query,
            options);

        GridLog.PageLoaded(_logger, DataItemName, pageNumber, page.Rows.Count, totalRows);
        await NotifyRowsLoadedAsync(request, page, cancellationToken).ConfigureAwait(false);

        return page;
    }

    private static readonly GridKeysetPage<TDataItem> NoRows = new([], null, null);

    // A keyset data source seeks from the cursor and returns the cursors of the page's edge rows; any other skips.
    private async Task<GridKeysetPage<TDataItem>> LoadPageAsync(
        GridDataSourceQuery<TDataItem> query, MaviGridCursor? cursor, int pageNumber, int pageSize, int totalRows, CancellationToken cancellationToken)
    {
        if (totalRows == 0)
        {
            return NoRows;
        }

        var pageQuery = query with { Skip = (pageNumber - 1) * pageSize, Take = pageSize };

        return _dataSource is IGridKeysetDataSource<TDataItem> keyset
            ? await keyset.GetKeysetPageAsync(pageQuery with { Cursor = cursor }, cancellationToken).ConfigureAwait(false)
            : new GridKeysetPage<TDataItem>(await _dataSource.GetPageAsync(pageQuery, cancellationToken).ConfigureAwait(false), null, null);
    }

    private static int TotalPages(int totalRows, int pageSize) => (int)((totalRows + (long)pageSize - 1) / pageSize);

    private static int ClampPage(int requested, int totalRows, int pageSize) => Math.Clamp(requested, 1, Math.Max(TotalPages(totalRows, pageSize), 1));

    private static int RowsOnPage(int totalRows, int pageNumber, int pageSize) => Math.Clamp(totalRows - (pageNumber - 1) * pageSize, 0, pageSize);

    private async ValueTask<AccessResult> EvaluateAccessAsync(GridAccessRequest request, CancellationToken cancellationToken)
    {
        var allowed = false;
        List<Expression<Func<TDataItem, bool>>>? rowFilters = null;
        HashSet<string>? hiddenColumns = null;

        foreach (var policy in _policies)
        {
            var decision = await policy.EvaluateAsync<TDataItem>(request, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Grid access policy '{policy.GetType().FullName}' returned no decision for '{typeof(TDataItem).FullName}'.");

            if (decision.Outcome == GridAccessOutcome.Denied)
            {
                return AccessResult.Deny(decision.DeniedMessage);
            }

            // Not-applicable decisions may hide columns too, without allowing any rows.
            if (decision.HiddenColumns.Count > 0)
            {
                (hiddenColumns ??= new HashSet<string>(StringComparer.Ordinal)).UnionWith(decision.HiddenColumns);
            }

            if (decision.Outcome == GridAccessOutcome.Allowed)
            {
                allowed = true;

                if (decision.RowFilter is not null)
                {
                    (rowFilters ??= []).Add(decision.RowFilter);
                }
            }
        }

        return !allowed && _options.RequireExplicitAllow ? AccessResult.Deny(null) : AccessResult.Allow(rowFilters, hiddenColumns);
    }

    private async Task<MaviGridCollection<TKey>> DenyAsync(
        GridAccessRequest request, string? message, GridModelMetadata<TDataItem, TModel> metadata, int pageSize, CancellationToken cancellationToken)
    {
        GridLog.FetchDenied(_logger, DataItemName, request.AccessTag);

        foreach (var observer in _observers)
        {
            await observer.OnAccessDeniedAsync<TDataItem>(new GridAccessDenied(request, message), cancellationToken).ConfigureAwait(false);
        }

        return new MaviGridCollection<TKey>
        {
            Columns = metadata.DeniedColumns,
            PageSize = pageSize,
            AccessDenied = true,
            AccessDeniedMessage = message
        };
    }

    private GridSort? ResolveSort(MaviGridQuery query, GridColumnSet columns)
    {
        if (query.SortColumnKey is null)
        {
            return null;
        }

        if (columns.TryGetPath(query.SortColumnKey, out var path))
        {
            return new GridSort(path.DataItemPath, query.SortOrder, path.NullDefault);
        }

        GridLog.SortIgnored(_logger, query.SortColumnKey, DataItemName);
        return null;
    }

    private Expression<Func<TDataItem, bool>>? CombineFilters(
        MaviGridQuery query,
        GridFetchOptions<TDataItem, TModel>? options,
        GridColumnSet columns,
        IReadOnlyList<Expression<Func<TDataItem, bool>>>? rowFilters)
    {
        var predicates = new List<Expression<Func<TDataItem, bool>>>(query.Filters.Count + _globalFilters.Length + 2);

        foreach (var filter in query.Filters)
        {
            if (columns.TryGetPath(filter.ColumnKey, out var path))
            {
                predicates.Add(BuildColumnFilter(filter, path));
            }
            else
            {
                GridLog.FilterIgnored(_logger, filter.ColumnKey, DataItemName);
            }
        }

        if (options?.Filter is not null)
        {
            predicates.Add(options.Filter);
        }

        foreach (var provider in _globalFilters)
        {
            if (provider.GetFilter<TDataItem>() is { } global)
            {
                predicates.Add(global);
            }
        }

        if (rowFilters is not null)
        {
            predicates.AddRange(rowFilters);
        }

        return PredicateCombiner.AndAll(predicates);
    }

    // A value the property's type can't hold (an int column given 99999999999, a date column given "2026-13-45")
    // matches no row, rather than failing the fetch on every keystroke.
    private Expression<Func<TDataItem, bool>> BuildColumnFilter(MaviGridColumnFilter filter, GridPropertyPath path)
    {
        try
        {
            return GridFilterBuilder<TDataItem>.Build(path.DataItemPath, filter.Criteria, filter.Value, path.NullDefault);
        }
        catch (FormatException)
        {
            GridLog.FilterValueRejected(_logger, filter.ColumnKey, DataItemName);
            return MatchNothing;
        }
    }

    private static readonly Expression<Func<TDataItem, bool>> MatchNothing = _ => false;

    private async ValueTask NotifyRowsLoadedAsync(GridAccessRequest request, MaviGridCollection<TKey> page, CancellationToken cancellationToken)
    {
        if (_observers.Length == 0)
        {
            return;
        }

        var keys = new TKey[page.Rows.Count];

        for (var i = 0; i < keys.Length; i++)
        {
            keys[i] = page.Rows[i].Key;
        }

        var loaded = new GridRowsLoaded<TKey>(request, keys, page.PageNumber, page.PageSize, page.TotalRows);

        foreach (var observer in _observers)
        {
            await observer.OnRowsLoadedAsync<TDataItem, TKey>(loaded, cancellationToken).ConfigureAwait(false);
        }
    }

    private readonly record struct AccessResult(
        bool Denied, string? DeniedMessage, IReadOnlyList<Expression<Func<TDataItem, bool>>>? RowFilters, IReadOnlyCollection<string>? HiddenColumns)
    {
        public static AccessResult Allow(IReadOnlyList<Expression<Func<TDataItem, bool>>>? rowFilters, IReadOnlyCollection<string>? hiddenColumns) =>
            new(false, null, rowFilters, hiddenColumns);

        public static AccessResult Deny(string? message) => new(true, message, null, null);
    }
}

using Maviray.Blazor.Components.Core.Enums;
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

        var columns = metadata.ColumnsFor(options?.PropertyPathMap);
        var sort = ResolveSort(query, columns);

        var dataSourceQuery = new GridDataSourceQuery<TDataItem>
        {
            Filter = CombineFilters(query, options, columns, access.RowFilters),
            Sort = sort,
            IncludePaths = metadata.IncludePaths,
            Include = options?.Include
        };

        var totalRows = await _dataSource.CountAsync(dataSourceQuery, cancellationToken).ConfigureAwait(false);
        var totalPages = (int)((totalRows + (long)pageSize - 1) / pageSize);
        var pageNumber = Math.Clamp(query.PageNumber, 1, Math.Max(totalPages, 1));

        IReadOnlyList<TDataItem> items = totalRows == 0
            ? []
            : await _dataSource.GetPageAsync(dataSourceQuery with { Skip = (pageNumber - 1) * pageSize, Take = pageSize }, cancellationToken).ConfigureAwait(false);

        var page = new MaviGridCollection<TKey>
        {
            Columns = columns.Columns,
            Rows = BuildRows(items, metadata, options?.CellThemes),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRows = totalRows,
            TotalPages = totalPages,
            SortColumnKey = sort is null ? null : query.SortColumnKey,
            SortOrder = query.SortOrder
        };

        GridLog.PageLoaded(_logger, DataItemName, pageNumber, page.Rows.Count, totalRows);
        await NotifyRowsLoadedAsync(request, page, cancellationToken).ConfigureAwait(false);

        return page;
    }

    private async ValueTask<AccessResult> EvaluateAccessAsync(GridAccessRequest request, CancellationToken cancellationToken)
    {
        var allowed = false;
        List<Expression<Func<TDataItem, bool>>>? rowFilters = null;

        foreach (var policy in _policies)
        {
            var decision = await policy.EvaluateAsync<TDataItem>(request, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Grid access policy '{policy.GetType().FullName}' returned no decision for '{typeof(TDataItem).FullName}'.");

            switch (decision.Outcome)
            {
                case GridAccessOutcome.Denied:
                    return AccessResult.Deny(decision.DeniedMessage);

                case GridAccessOutcome.Allowed:
                    allowed = true;

                    if (decision.RowFilter is not null)
                    {
                        (rowFilters ??= []).Add(decision.RowFilter);
                    }

                    break;
            }
        }

        return !allowed && _options.RequireExplicitAllow ? AccessResult.Deny(null) : AccessResult.Allow(rowFilters);
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
                predicates.Add(GridFilterBuilder<TDataItem>.Build(path.DataItemPath, filter.Criteria, filter.Value, path.NullDefault));
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

    private static MaviGridRow<TKey>[] BuildRows(
        IReadOnlyList<TDataItem> items,
        GridModelMetadata<TDataItem, TModel> metadata,
        IReadOnlyDictionary<string, Func<object?, ThemeColorScheme>>? cellThemes)
    {
        var descriptors = metadata.Descriptors;
        var rows = new MaviGridRow<TKey>[items.Count];

        for (var r = 0; r < items.Count; r++)
        {
            var item = items[r];
            var model = metadata.MapToModel(item);
            model.ConstructFrom(item);

            var cells = new MaviGridCell[descriptors.Count];

            for (var c = 0; c < descriptors.Count; c++)
            {
                var descriptor = descriptors[c];

                if (!descriptor.TryGetValue(model, out var value))
                {
                    cells[c] = MaviGridCell.Empty;
                    continue;
                }

                var theme = descriptor.GraphicDisplay && cellThemes is not null && cellThemes.TryGetValue(descriptor.Key, out var resolve)
                    ? resolve(value)
                    : default;

                cells[c] = new MaviGridCell(descriptor.Format(value), theme);
            }

            var actions = model.GetContextActions();

            rows[r] = new MaviGridRow<TKey>
            {
                Key = model.Key,
                Cells = cells,
                ContextActions = actions switch
                {
                    null => [],
                    IReadOnlyList<IMaviRowContextMenuItem<TKey>> list => list,
                    _ => actions.ToArray()
                }
            };
        }

        return rows;
    }

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

    private readonly record struct AccessResult(bool Denied, string? DeniedMessage, IReadOnlyList<Expression<Func<TDataItem, bool>>>? RowFilters)
    {
        public static AccessResult Allow(IReadOnlyList<Expression<Func<TDataItem, bool>>>? rowFilters) => new(false, null, rowFilters);

        public static AccessResult Deny(string? message) => new(true, message, null);
    }
}

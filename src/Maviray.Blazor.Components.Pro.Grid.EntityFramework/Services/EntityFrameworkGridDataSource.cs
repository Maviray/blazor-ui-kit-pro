using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Services;

/// <summary>
/// Reads grid pages with EF Core. Each call runs on its own short-lived context, without change tracking, and
/// sends only the current page to the database: <c>ORDER BY … LIMIT/OFFSET</c> (or <c>OFFSET/FETCH</c>), with the
/// filter values as parameters.
/// </summary>
/// <remarks>
/// The order always ends with the entity's primary key (in the sort direction) unless the sort already is the whole
/// key, so paging is stable when sort values repeat. Without a requested sort, rows are ordered by the primary key.
/// Keyless entity types and keys with shadow properties are paged in the requested order only.
/// </remarks>
public sealed partial class EntityFrameworkGridDataSource<TDataItem>(
    IGridDbContextFactory contexts,
    ILogger<EntityFrameworkGridDataSource<TDataItem>> logger) : IGridDataSource<TDataItem>
    where TDataItem : class
{
    private static readonly string DataItemName = typeof(TDataItem).Name;

    public async Task<int> CountAsync(GridDataSourceQuery<TDataItem> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var context = await contexts.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<TDataItem> source = context.Set<TDataItem>().AsNoTracking();

        // The shaper can change which rows exist (for example IgnoreQueryFilters()); EF drops its includes for a count.
        if (query.Include is not null)
        {
            source = query.Include(source);
        }

        LogCounting(logger, DataItemName, query.Filter);

        return await Filtered(source, query.Filter).CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TDataItem>> GetPageAsync(GridDataSourceQuery<TDataItem> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var context = await contexts.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<TDataItem> source = context.Set<TDataItem>().AsNoTracking();

        foreach (var path in query.IncludePaths)
        {
            source = source.Include(path);
        }

        if (query.Include is not null)
        {
            source = query.Include(source);
        }

        source = Order(Filtered(source, query.Filter), query.Sort, PrimaryKey(context));

        LogLoadingPage(logger, DataItemName, query.Skip, query.Take, query.Sort?.Path, query.Sort?.Order, query.Filter);

        return await source.Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IQueryable<TDataItem> Filtered(IQueryable<TDataItem> source, Expression<Func<TDataItem, bool>>? filter) =>
        filter is null ? source : source.Where(filter);

    private static IQueryable<TDataItem> Order(IQueryable<TDataItem> source, GridSort? sort, IReadOnlyList<string> key)
    {
        if (sort is null)
        {
            return key.Count == 0 ? source : ThenByKey(source.OrderByDynamic(key[0], SortOrder.Ascending), key, 1, SortOrder.Ascending);
        }

        var ordered = source.OrderByDynamic(sort.Path, sort.Order, sort.NullDefault);
        var sortIsWholeKey = key.Count == 1 && string.Equals(key[0], sort.Path, StringComparison.Ordinal);

        return sortIsWholeKey ? ordered : ThenByKey(ordered, key, 0, sort.Order);
    }

    private static IOrderedQueryable<TDataItem> ThenByKey(IOrderedQueryable<TDataItem> ordered, IReadOnlyList<string> key, int start, SortOrder order)
    {
        for (var i = start; i < key.Count; i++)
        {
            ordered = ordered.ThenByDynamic(key[i], order);
        }

        return ordered;
    }

    private static IReadOnlyList<string> PrimaryKey(DbContext context)
    {
        var key = context.Model.FindEntityType(typeof(TDataItem))?.FindPrimaryKey();

        return key is null || key.Properties.Any(p => p.IsShadowProperty())
            ? []
            : key.Properties.Select(p => p.Name).ToArray();
    }

    [LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = "Counting {DataItemType} rows. Filter: {Filter}")]
    private static partial void LogCounting(ILogger logger, string dataItemType, Expression? filter);

    [LoggerMessage(EventId = 102, Level = LogLevel.Debug,
        Message = "Loading {DataItemType} rows {Skip}+{Take} ordered by {OrderBy} {SortOrder}. Filter: {Filter}")]
    private static partial void LogLoadingPage(ILogger logger, string dataItemType, int skip, int take, string? orderBy, SortOrder? sortOrder, Expression? filter);
}

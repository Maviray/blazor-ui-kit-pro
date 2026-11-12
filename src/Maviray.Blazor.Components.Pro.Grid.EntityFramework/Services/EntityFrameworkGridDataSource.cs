using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Services;

/// <summary>
/// Reads grid pages with EF Core. Each call runs on its own short-lived context, without change tracking, and
/// sends only the current page to the database: <c>ORDER BY … LIMIT/OFFSET</c> (or <c>OFFSET/FETCH</c>), with the
/// filter values as parameters.
/// </summary>
/// <remarks>
/// <para>The order always ends with the entity's primary key (in the sort direction) unless the sort already is the whole
/// key, so paging is stable when sort values repeat. Without a requested sort, rows are ordered by the primary key.
/// Keyless entity types and keys with shadow properties are paged in the requested order only.</para>
/// <para>Pages loaded through <see cref="GetKeysetPageAsync"/> carry cursors, and a cursor seeks with
/// <c>WHERE (sort, key) &gt; (…)</c> instead of an offset when every order key can be sought: the sort is a column of
/// the entity itself that is required (or has a null default), the key and sort types are numbers, strings, bools,
/// Guids, enums, dates or times, and none has a value converter (except an enum stored as its number). Otherwise the
/// page is skipped to as usual and carries no cursors.</para>
/// </remarks>
public sealed partial class EntityFrameworkGridDataSource<TDataItem>(
    IGridDbContextFactory contexts,
    ILogger<EntityFrameworkGridDataSource<TDataItem>> logger) : IGridKeysetDataSource<TDataItem>
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

        return await SkipToPageAsync(context, PageSource(context, query), query, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GridKeysetPage<TDataItem>> GetKeysetPageAsync(GridDataSourceQuery<TDataItem> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var context = await contexts.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var source = PageSource(context, query);

        if (Keyset(context, query.Sort) is not { } keyset)
        {
            return new GridKeysetPage<TDataItem>(await SkipToPageAsync(context, source, query, cancellationToken).ConfigureAwait(false), null, null);
        }

        if (query.Cursor is { } cursor)
        {
            LogSeekingPage(logger, DataItemName, cursor.Direction, query.Take, query.Sort?.Path, query.Sort?.Order, query.Filter);
        }
        else
        {
            LogLoadingPage(logger, DataItemName, query.Skip, query.Take, query.Sort?.Path, query.Sort?.Order, query.Filter);
        }

        // The cursors are read from the loaded rows, and the order keys are the sort (a column path) and the key.
        var select = SelectPaths(context, query);

        return await keyset.LoadPageAsync(source, query.Cursor, query.Skip, query.Take,
            (q, ct) => (select is null ? q : q.SelectPaths(select)).ToListAsync(ct), cancellationToken).ConfigureAwait(false);
    }

    // The projection's members plus the primary key, which orders every page and positions its cursors.
    private static IReadOnlyCollection<string>? SelectPaths(DbContext context, GridDataSourceQuery<TDataItem> query) =>
        query.SelectPaths is { } paths ? [.. paths.Union(PrimaryKey(context), StringComparer.Ordinal)] : null;

    // Includes, the shaper and the filter: everything but the order and the slice.
    private static IQueryable<TDataItem> PageSource(DbContext context, GridDataSourceQuery<TDataItem> query)
    {
        IQueryable<TDataItem> source = context.Set<TDataItem>().AsNoTracking();

        // A projection loads related members itself; including whole navigations would only be ignored.
        foreach (var path in query.SelectPaths is null ? query.IncludePaths : [])
        {
            source = source.Include(path);
        }

        if (query.Include is not null)
        {
            source = query.Include(source);
        }

        return Filtered(source, query.Filter);
    }

    private async Task<List<TDataItem>> SkipToPageAsync(DbContext context, IQueryable<TDataItem> source, GridDataSourceQuery<TDataItem> query, CancellationToken cancellationToken)
    {
        LogLoadingPage(logger, DataItemName, query.Skip, query.Take, query.Sort?.Path, query.Sort?.Order, query.Filter);

        var page = Order(source, query.Sort, PrimaryKey(context)).Skip(query.Skip).Take(query.Take);

        return await (SelectPaths(context, query) is { } select ? page.SelectPaths(select) : page).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The same order as <see cref="Order"/> as a keyset, or <see langword="null"/> when a key can't be sought.</summary>
    private static GridKeyset<TDataItem>? Keyset(DbContext context, GridSort? sort)
    {
        var entityType = context.Model.FindEntityType(typeof(TDataItem));

        if (entityType?.FindPrimaryKey()?.Properties is not { } keyProperties
            || keyProperties.Any(p => p.IsShadowProperty() || !GridKeyset<TDataItem>.CanSeek(p.ClrType) || !OrdersAsItsClrType(p)))
        {
            return null;
        }

        var direction = sort?.Order ?? SortOrder.Ascending;
        var keys = new List<GridOrderKey>(keyProperties.Count + 1);
        var sortIsWholeKey = sort is not null && keyProperties.Count == 1 && string.Equals(keyProperties[0].Name, sort.Path, StringComparison.Ordinal);

        if (sort is not null && !sortIsWholeKey)
        {
            // Only a column of the entity itself: through a navigation the value can be missing (an outer join), and
            // NULLs don't compare. A nullable column needs a null default for the same reason.
            if (entityType.FindProperty(sort.Path) is not { } property || !OrdersAsItsClrType(property)
                || (property.IsNullable && sort.NullDefault is null))
            {
                return null;
            }

            keys.Add(new GridOrderKey(sort.Path, sort.Order, sort.NullDefault));
        }

        keys.AddRange(keyProperties.Select(p => new GridOrderKey(p.Name, direction)));

        return GridKeyset<TDataItem>.TryCreate(keys, out var keyset) ? keyset : null;
    }

    // A value converter changes what the database orders (an enum stored as its name sorts alphabetically), so the
    // seek's comparison would disagree with the ORDER BY. Only an enum stored as its underlying number is kept.
    private static bool OrdersAsItsClrType(IReadOnlyProperty property)
    {
        var converter = property.GetValueConverter() ?? property.FindTypeMapping()?.Converter;

        if (converter is null)
        {
            return true;
        }

        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        var providerType = Nullable.GetUnderlyingType(converter.ProviderClrType) ?? converter.ProviderClrType;

        return clrType.IsEnum && providerType == Enum.GetUnderlyingType(clrType);
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

    [LoggerMessage(EventId = 103, Level = LogLevel.Debug,
        Message = "Seeking the {Take} {DataItemType} rows {Direction} a cursor, ordered by {OrderBy} {SortOrder}. Filter: {Filter}")]
    private static partial void LogSeekingPage(ILogger logger, string dataItemType, MaviGridCursorDirection direction, int take, string? orderBy, SortOrder? sortOrder, Expression? filter);
}

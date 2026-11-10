using Maviray.Blazor.Components.Core.Enums;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>One request to an <see cref="IGridDataSource{TDataItem}"/>: the combined filter, the sort and the slice of rows.</summary>
public sealed record GridDataSourceQuery<TDataItem> where TDataItem : class
{
    /// <summary>
    /// Every row constraint of the fetch, already AND-combined: column filters, the grid's own filter, global filters
    /// and access-policy row filters. It applies to the count and to the page. Filter values are carried as captured
    /// values, so a relational provider sends them as query parameters.
    /// </summary>
    public Expression<Func<TDataItem, bool>>? Filter { get; init; }

    /// <summary>The requested order, or <see langword="null"/> for the data source's default (its primary key).</summary>
    public GridSort? Sort { get; init; }

    public int Skip { get; init; }

    public int Take { get; init; }

    /// <summary>Navigation paths to eager-load, possibly dotted (for example <c>"Order.Customer"</c>).</summary>
    public IReadOnlyCollection<string> IncludePaths { get; init; } = [];

    /// <summary>Shapes the root query before filtering, ordering and paging, in addition to <see cref="IncludePaths"/>.</summary>
    public GridIncludeConfigurator<TDataItem>? Include { get; init; }
}

/// <summary>
/// Sort by a dot-path on the data item. When <see cref="NullDefault"/> is set, a <see langword="null"/> at the path
/// sorts as that value.
/// </summary>
public sealed record GridSort(string Path, SortOrder Order, object? NullDefault = null);

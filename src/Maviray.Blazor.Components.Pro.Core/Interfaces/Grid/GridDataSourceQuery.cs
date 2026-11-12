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

    /// <summary>
    /// For <see cref="IGridKeysetDataSource{TDataItem}"/>: load the <see cref="Take"/> rows after or before this
    /// position instead of skipping <see cref="Skip"/> rows. A cursor the data source can't use is ignored.
    /// </summary>
    public Models.Grid.MaviGridCursor? Cursor { get; init; }

    /// <summary>Navigation paths to eager-load, possibly dotted (for example <c>"Order.Customer"</c>).</summary>
    public IReadOnlyCollection<string> IncludePaths { get; init; } = [];

    /// <summary>
    /// When set, load only these members (dot-paths) into new data items, after ordering and paging, for example with
    /// <see cref="Extensions.QueryableExtensions.SelectPaths{T}"/>; <see cref="IncludePaths"/> is then empty. Keep
    /// whatever else the data source needs, such as the order keys a cursor reads.
    /// </summary>
    public IReadOnlyCollection<string>? SelectPaths { get; init; }

    /// <summary>Shapes the root query before filtering, ordering and paging, in addition to <see cref="IncludePaths"/>.</summary>
    public GridIncludeConfigurator<TDataItem>? Include { get; init; }
}

/// <summary>
/// Sort by a dot-path on the data item. When <see cref="NullDefault"/> is set, a <see langword="null"/> at the path
/// sorts as that value.
/// </summary>
public sealed record GridSort(string Path, SortOrder Order, object? NullDefault = null);

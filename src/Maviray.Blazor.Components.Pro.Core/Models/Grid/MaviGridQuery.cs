using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// What the grid asks for in one fetch: the page, the sort and the column filters. Immutable, so every fetch works
/// on its own snapshot.
/// </summary>
public sealed record MaviGridQuery
{
    /// <summary>One-based page number. Values below 1 mean the first page; values past the end mean the last page.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Rows per page, clamped by the factory to <c>GridDataFactoryOptions.MaxPageSize</c>.</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>Key of the column to sort by; <see langword="null"/> uses the data source's default order.</summary>
    public string? SortColumnKey { get; init; }

    public SortOrder SortOrder { get; init; } = SortOrder.Ascending;

    /// <summary>Column filters, AND-combined. Filters on keys that are not filterable columns are ignored.</summary>
    public IReadOnlyList<MaviGridColumnFilter> Filters { get; init; } = [];
}

/// <summary>A filter on one column. <see cref="Value"/> is read in the invariant culture for the column's type.</summary>
public sealed record MaviGridColumnFilter(string ColumnKey, MaviGridFilterCriteria Criteria, string Value);

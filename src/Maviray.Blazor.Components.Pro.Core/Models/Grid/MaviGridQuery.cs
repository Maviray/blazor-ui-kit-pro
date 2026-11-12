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

    /// <summary>
    /// The total the grid's current page reported, sent when the filters haven't changed since that page (only the
    /// page, the page size or the sort did). Fetch code may use it instead of counting again. It is
    /// <see langword="null"/> on the first fetch, after a filter change, on <c>RefreshAsync</c>, and after a denied or
    /// failed fetch.
    /// </summary>
    /// <remarks>
    /// Rows added or removed since the count make it stale; check that the loaded page has the number of rows the
    /// total predicts, and count again when it doesn't.
    /// </remarks>
    public int? KnownTotalRows { get; init; }

    /// <summary>
    /// Set when the user moves to the next or previous page (other than the first) from a page that returned a
    /// cursor, with the same sort, filters and page size. Fetch code that pages by keyset loads the rows after or
    /// before it instead of skipping <c>(PageNumber - 1) * PageSize</c> rows, which costs the same on every page.
    /// <see cref="PageNumber"/> is still the target page; fetch code that doesn't seek ignores the cursor.
    /// </summary>
    public MaviGridCursor? Cursor { get; init; }
}

/// <summary>A filter on one column. <see cref="Value"/> is read in the invariant culture for the column's type.</summary>
public sealed record MaviGridColumnFilter(string ColumnKey, MaviGridFilterCriteria Criteria, string Value);

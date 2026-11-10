using Maviray.Blazor.Components.Core.Enums;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>One page of a grid: its columns, the rows of the page and the paging totals.</summary>
public sealed class MaviGridCollection<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>Columns in display order; <see cref="MaviGridColumn.Index"/> is the position here and in every row's cells.</summary>
    public required IReadOnlyList<MaviGridColumn> Columns { get; init; }

    public IReadOnlyList<MaviGridRow<TKey>> Rows { get; init; } = [];

    /// <summary>The one-based page these rows belong to, after clamping to the available pages.</summary>
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; }

    public int TotalRows { get; init; }

    public int TotalPages { get; init; }

    /// <summary>The column the rows are sorted by, or <see langword="null"/> for the data source's default order.</summary>
    public string? SortColumnKey { get; init; }

    public SortOrder SortOrder { get; init; }

    /// <summary>The fetch was denied by an access policy: there are no rows and the totals are zero.</summary>
    public bool AccessDenied { get; init; }

    /// <summary>Optional text from the denying policy, shown to the user.</summary>
    public string? AccessDeniedMessage { get; init; }

    /// <summary>One-based number of the first row on this page, or 0 when there are no rows.</summary>
    public int FirstRowNumber => TotalRows == 0 ? 0 : (PageNumber - 1) * PageSize + 1;

    /// <summary>One-based number of the last row on this page, or 0 when there are no rows.</summary>
    public int LastRowNumber => TotalRows == 0 ? 0 : Math.Min(PageNumber * PageSize, TotalRows);
}

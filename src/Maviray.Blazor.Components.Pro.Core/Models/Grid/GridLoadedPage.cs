namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// One page of data items an app loaded itself, for <see cref="Services.GridPageBuilder{TDataItem, TModel, TKey}"/>.
/// </summary>
/// <param name="Items">The rows of the page, in display order.</param>
/// <param name="PageNumber">The one-based page the items are, after any clamping to the available pages.</param>
/// <param name="PageSize">The page size the items were loaded with.</param>
/// <param name="TotalRows">How many rows match the filters in all.</param>
public sealed record GridLoadedPage<TDataItem>(IReadOnlyList<TDataItem> Items, int PageNumber, int PageSize, int TotalRows)
{
    /// <summary>Becomes <see cref="MaviGridCollection{TKey}.NextPageCursor"/>.</summary>
    public string? NextPageCursor { get; init; }

    /// <summary>Becomes <see cref="MaviGridCollection{TKey}.PreviousPageCursor"/>.</summary>
    public string? PreviousPageCursor { get; init; }

    /// <summary>
    /// Keys of columns the user may not see: they are left out of the page's columns and cells, and its sort isn't
    /// reported for them. The app's query code must also ignore sorting and filtering on them.
    /// </summary>
    public IReadOnlyCollection<string>? HiddenColumns { get; init; }
}

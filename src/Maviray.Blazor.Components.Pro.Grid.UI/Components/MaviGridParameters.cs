using Maviray.Blazor.Components.Core.Models.Tables;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Components;

/// <summary>
/// Appearance and behaviour of a <see cref="MaviGrid{TKey}"/>. The initial sort (<see cref="SortColumnKey"/> with
/// <see cref="TableParameters.SortOrder"/>) and <see cref="PageSize"/> apply when the grid starts; after that the
/// user's choices win.
/// </summary>
public sealed class MaviGridParameters : TableParameters
{
    /// <summary>Column the first page is sorted by; <see langword="null"/> uses the data source's default order.</summary>
    public string? SortColumnKey { get; set; }

    /// <summary>Rows per page when the grid starts.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>The page sizes offered in the pager.</summary>
    public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50, 100];

    /// <summary>Pause, in milliseconds, after the user stops typing in a column filter before the grid fetches.</summary>
    public int FilterDebounceMilliseconds { get; set; } = 400;

    /// <summary>Shown when an access policy denied the fetch and gave no message.</summary>
    public string? AccessDeniedTitle { get; set; } = "You do not have access to this data";

    /// <summary>Shown when the fetch failed.</summary>
    public string? FetchErrorTitle { get; set; } = "Data could not be loaded";

    /// <summary>
    /// Column key to weight: these columns share the grid's free width in proportion to their weights, in place of the
    /// model's <c>[GridStretch]</c> marks. <see langword="null"/> uses the marks; an empty dictionary stretches no data
    /// column, so the actions column takes the free width. Keys are looked up with the dictionary's own comparer, and
    /// unknown keys are ignored. Weights must be at least 1.
    /// </summary>
    /// <remarks>
    /// Read when the grid's parameters are set, like <see cref="TableParameters.Condensed"/>. To change it, assign a new
    /// dictionary: one changed in place isn't noticed.
    /// </remarks>
    public IReadOnlyDictionary<string, int>? StretchColumns { get; set; }

    /// <summary>
    /// When <see langword="true"/> (the default), the grid fills the width of its parent element. When
    /// <see langword="false"/>, it shrinks to its columns, and stretching columns have no visible effect.
    /// </summary>
    public bool FillWidth { get; set; } = true;

    /// <summary>
    /// When <see langword="false"/>, the grid doesn't fetch during static prerendering and shows a placeholder
    /// instead; the interactive grid then fetches once. Use it to avoid querying (and auditing) every page view twice.
    /// </summary>
    public bool FetchWhilePrerendering { get; set; } = true;
}

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
    /// When <see langword="false"/>, the grid doesn't fetch during static prerendering and shows a placeholder
    /// instead; the interactive grid then fetches once. Use it to avoid querying (and auditing) every page view twice.
    /// </summary>
    public bool FetchWhilePrerendering { get; set; } = true;
}

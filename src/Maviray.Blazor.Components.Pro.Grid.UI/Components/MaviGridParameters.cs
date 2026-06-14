using Maviray.Blazor.Components.Core.Models.Tables;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Components;

public class MaviGridParameters : TableParameters
{
    /// <summary>
    /// The amount of time, in milliseconds, to wait after the user stops typing in a column
    /// filter before the filter is applied and the grid is refreshed.
    /// </summary>
    public int FilterDebounceMilliseconds { get; set; } = 400;
}
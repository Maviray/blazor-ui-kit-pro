using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;

public class GridFilterModel
{
    /// <summary>
    /// Name of Property 
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Value to match
    /// </summary>
    public string? Value { get; set; }

    public MaviGridFilterCriteria GridFilterCriteria { get; set; }
}
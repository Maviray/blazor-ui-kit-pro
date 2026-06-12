using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;

namespace Maviray.Blazor.Components.Pro.Core.Enums.Grid;

public class MaviGridDataQueryParameters
{
    public SortOrder SortOrder { get; set; }
    public int NumberOfItemsPerPage { get; set; }
    public int PageNumber { get; set; }
    public string? OrderByColumnKey { get; set; }
    public IEnumerable<GridFilterModel>? Filters { get; set; }
}
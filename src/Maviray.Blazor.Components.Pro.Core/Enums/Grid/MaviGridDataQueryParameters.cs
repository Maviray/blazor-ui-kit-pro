using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;

namespace Maviray.Blazor.Components.Pro.Core.Enums.Grid;

public class MaviGridDataQueryParameters
{
    public SortOrder SortOrder { get; set; } = SortOrder.Ascending;
    public int NumberOfItemsPerPage { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
    public string? OrderByColumnKey { get; set; }
    public IEnumerable<GridFilterModel>? Filters { get; set; }
}
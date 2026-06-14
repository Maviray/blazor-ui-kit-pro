using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

public class MaviGridColumn
{
    public string Guid { get; init; } = System.Guid.NewGuid().ToString();
    public string? Title { get; set; }
    public string Key { get; set; } = System.Guid.NewGuid().ToString(); // data item property name
    public bool Active { get; set; }
    public bool Visible { get; set; } = true;
    public int Sequence { get; set; }
    public SortOrder SortOrder { get; set; } 
    public MaviGridColumnCellBehavior MaviGridColumnBehavior { get; set; }
    public MaviGridColumnType GridColumnType { get; set; }
    public MaviGridPropertyType GridPropertyType { get; set; }
    public MaviGridFilterCriteria CurrentFilter { get; set; }
    public HorizontalPosition HorizontalTextAlignment { get; set; }
    public string? CurrentFilerValue { get; set; }
    public string? BoolPositiveDisplay { get; set; } // displays value when column is of boolean type and value is true/selected
    public string? BoolNegativeDisplay { get; set; } // displays value when column is of boolean type and value is false/unselected

    public Dictionary<string, string>? PossibleFilterValues { get; set; } = [];
}
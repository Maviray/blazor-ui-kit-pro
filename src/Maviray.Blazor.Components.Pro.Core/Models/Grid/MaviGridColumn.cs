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
    public object? CurrentFilerValue { get; set; }
    public string? BoolPositiveDisplay { get; set; } // displays value when column is of boolean type and value is true/selected
    public string? BoolNegativeDisplay { get; set; } // displays value when column is of boolean type and value is false/unselected
    public string? DateTimeFormat { get; set; } // custom format applied to DateTime/DateTimeOffset cells; null uses the default

    // When true, the column's cells should be rendered with an advanced/graphical presentation
    // (e.g. a badge) instead of plain text. Driven by GridAdvancedDisplayAttribute on the model property.
    public bool GraphicDisplay { get; set; }

    // When true, the column's header gets a clickable sort affordance. False when the property could
    // not be resolved to a TDataItem path (see GridPropertyPathMap) - set once per column at build time.
    public bool Sortable { get; set; } = true;

    // When true, the column renders a filter input. False when the property could not be resolved to
    // a TDataItem path (see GridPropertyPathMap) - set once per column at build time.
    public bool Filterable { get; set; } = true;

    public Dictionary<string, string>? PossibleFilterValues { get; set; } = [];
}
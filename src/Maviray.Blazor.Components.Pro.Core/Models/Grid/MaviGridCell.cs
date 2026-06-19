using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

public class MaviGridCell<TKey> where TKey : IEquatable<TKey>
{
    public string? Value { get; set; }
    public int? Sequence { get; set; }
    public string? ColumnGuid { get; set; }
    public string? ColumnKey { get; set; }
    public TKey? RowKey { get; set; }
    public MaviGridColumnType GridColumnType { get; set; }
    public MaviGridPropertyType GridPropertyType { get; set; }
    public bool Selected { get; set; }
    public bool Active { get; set; }
    public MaviGridColumnCellBehavior CellBehavior { get; set; }

    // Resolved per cell (only for columns with GraphicDisplay) via a consumer-supplied resolver,
    // signalling which colour scheme an advanced/graphical renderer should use for this cell.
    public ThemeColorScheme ThemeColorScheme { get; set; }
}
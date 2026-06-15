using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;

public class CellClickParameters<TKey> where TKey : IEquatable<TKey>
{
    public TKey? RowKey { get; set; }
    public string? ColumnKey { get; set; }
    public MaviGridColumnCellBehavior CellBehavior { get; set; }
}
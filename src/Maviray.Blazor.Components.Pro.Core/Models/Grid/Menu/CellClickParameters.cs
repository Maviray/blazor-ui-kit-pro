using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;

/// <summary>The cell a user clicked: its row key, its column key and the column's cell behaviour.</summary>
public sealed record CellClickParameters<TKey>(TKey RowKey, string ColumnKey, MaviGridColumnCellBehavior CellBehavior)
    where TKey : IEquatable<TKey>;

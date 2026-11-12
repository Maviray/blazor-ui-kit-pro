using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>One grid row: its key, one cell per column, and the row's actions.</summary>
public sealed class MaviGridRow<TKey> where TKey : IEquatable<TKey>
{
    public required TKey Key { get; init; }

    /// <summary>One cell per column, in column order: the cell of a column is <c>Cells[column.Index]</c>.</summary>
    public required IReadOnlyList<MaviGridCell> Cells { get; init; }

    public IReadOnlyList<IMaviRowContextMenuItem<TKey>> ContextActions { get; init; } = [];
}

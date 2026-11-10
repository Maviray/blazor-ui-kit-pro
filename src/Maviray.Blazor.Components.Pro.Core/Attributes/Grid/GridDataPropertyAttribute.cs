using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

/// <summary>
/// Makes a model property a grid column. Columns are ordered by <see cref="Sequence"/>; equal sequences keep
/// their declaration order.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GridDataPropertyAttribute(int sequence, MaviGridColumnCellBehavior cellBehavior = MaviGridColumnCellBehavior.Default) : Attribute
{
    public int Sequence { get; } = sequence;

    public MaviGridColumnCellBehavior CellBehavior { get; } = cellBehavior;
}

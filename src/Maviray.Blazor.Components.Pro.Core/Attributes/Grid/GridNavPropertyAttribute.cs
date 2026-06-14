using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

[AttributeUsage(AttributeTargets.Property)]
public class GridNavPropertyAttribute : Attribute
{
    public int Sequence { get; init; }
    public MaviGridColumnCellBehavior CellType { get; init; }

    public GridNavPropertyAttribute(int sequence)
    {
        Sequence = sequence;
        CellType = MaviGridColumnCellBehavior.Default;
    }

    public GridNavPropertyAttribute(int sequence, MaviGridColumnCellBehavior cellType)
    {
        Sequence = sequence;
        CellType = cellType;
    }
}
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

[AttributeUsage(AttributeTargets.Property)]
public class GridNavPropertyAttribute : Attribute
{
    public int Sequence { get; init; }
    public MaviGridCellType CellType { get; init; }

    public GridNavPropertyAttribute(int sequence)
    {
        Sequence = sequence;
        CellType = MaviGridCellType.Default;
    }

    public GridNavPropertyAttribute(int sequence, MaviGridCellType cellType)
    {
        Sequence = sequence;
        CellType = cellType;
    }
}
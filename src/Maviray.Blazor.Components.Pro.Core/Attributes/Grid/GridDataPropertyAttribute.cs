using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

[AttributeUsage(AttributeTargets.Property)]
public class GridDataPropertyAttribute : Attribute
{
    public int Sequence { get; init; }
    public MaviGridCellType CellType { get; init; }

    public GridDataPropertyAttribute(int sequence)
    {
        Sequence = sequence;
        CellType = MaviGridCellType.Default;
    }

    public GridDataPropertyAttribute(int sequence, MaviGridCellType cellType)
    {
        Sequence = sequence;
        CellType = cellType;
    }
}
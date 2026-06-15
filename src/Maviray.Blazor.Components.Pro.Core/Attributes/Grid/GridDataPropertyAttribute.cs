using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

[AttributeUsage(AttributeTargets.Property)]
public class GridDataPropertyAttribute : Attribute
{
    public int Sequence { get; init; }
    public MaviGridColumnCellBehavior ColumnCellBehavior { get; init; }

    public GridDataPropertyAttribute(int sequence)
    {
        Sequence = sequence;
        ColumnCellBehavior = MaviGridColumnCellBehavior.Default;
    }

    public GridDataPropertyAttribute(int sequence, MaviGridColumnCellBehavior cellType)
    {
        Sequence = sequence;
        ColumnCellBehavior = cellType;
    }
}
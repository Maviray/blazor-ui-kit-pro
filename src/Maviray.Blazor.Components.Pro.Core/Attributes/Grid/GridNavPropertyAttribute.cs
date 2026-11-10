using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

/// <summary>
/// Marks a model property that holds a related model. The navigation is eager-loaded under the same name from
/// the data item, and each of the related model's <see cref="GridNestedPropertyAttribute"/> properties becomes a
/// column keyed <c>Navigation.Property</c>, placed at <see cref="Sequence"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GridNavPropertyAttribute(int sequence, MaviGridColumnCellBehavior cellBehavior = MaviGridColumnCellBehavior.Default) : Attribute
{
    public int Sequence { get; } = sequence;

    public MaviGridColumnCellBehavior CellBehavior { get; } = cellBehavior;
}

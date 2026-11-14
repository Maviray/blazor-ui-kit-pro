namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

/// <summary>
/// Makes a column take a share of the grid's free width. On a <see cref="GridNavPropertyAttribute"/> property it applies
/// to each of the navigation's nested columns; a nested property's own mark overrides it. When no visible column
/// stretches, the grid's actions column takes the free width.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GridStretchAttribute(int weight = 1) : Attribute
{
    /// <summary>The column's share of the free width relative to other stretching columns. At least 1.</summary>
    public int Weight { get; } = weight;
}

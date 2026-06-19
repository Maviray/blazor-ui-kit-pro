namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

/// <summary>
/// Marks a model property whose grid column should be rendered using an advanced, graphical
/// presentation (for example a badge) rather than plain text. The factory reflects this attribute
/// onto <see cref="Models.Grid.MaviGridColumn.GraphicDisplay"/>. The per-cell appearance
/// (colour) is resolved separately via a consumer-supplied resolver.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class GridAdvancedDisplayAttribute : Attribute
{
}

namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

[AttributeUsage(AttributeTargets.Property)]
public class GridNestedPropertyAttribute : Attribute
{
    public string? Title { get; init; }

    public GridNestedPropertyAttribute(string title)
    {
        Title = title;
    }
}
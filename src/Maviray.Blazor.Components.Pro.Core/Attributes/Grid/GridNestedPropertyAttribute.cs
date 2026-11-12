namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

/// <summary>
/// Makes a property of a related model (one referenced through <see cref="GridNavPropertyAttribute"/>) a grid
/// column with the given header title.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GridNestedPropertyAttribute(string title) : Attribute
{
    public string Title { get; } = title;
}

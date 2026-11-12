namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

/// <summary>
/// Text shown for <see langword="true"/> and <see langword="false"/> in a boolean column. The column filter
/// matches these texts as well.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GridBooleanDisplayValuesAttribute(string trueText, string falseText) : Attribute
{
    public string TrueText { get; } = trueText;

    public string FalseText { get; } = falseText;
}

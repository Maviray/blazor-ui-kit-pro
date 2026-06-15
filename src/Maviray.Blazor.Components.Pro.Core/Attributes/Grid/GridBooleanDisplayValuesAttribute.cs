namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

[AttributeUsage(AttributeTargets.Property)]
public class GridBooleanDisplayValuesAttribute : Attribute
{
    public string Positive { get; init; }
    public string Negative { get; init; }

    public GridBooleanDisplayValuesAttribute(string positive, string negative)
    {
        Positive = positive;
        Negative = negative;
    }
}
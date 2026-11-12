namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

/// <summary>
/// Format string (invariant culture) for a date or time column, for example <c>"yyyy-MM-dd"</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GridDateTimeFormatAttribute(string format) : Attribute
{
    public string Format { get; } = format;
}

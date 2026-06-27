namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

[AttributeUsage(AttributeTargets.Property)]
public class GridDateTimeFormatAttribute : Attribute
{
    // Custom format string applied when rendering DateTime / DateTimeOffset cells, e.g. "yyyy-MM-dd"
    // to omit a time part that is always zero. When unset, the factory's default format is used.
    public string Format { get; init; }

    public GridDateTimeFormatAttribute(string format)
    {
        Format = format;
    }
}

namespace Maviray.Blazor.Components.Pro.Core.Enums.Grid;

/// <summary>
/// Category a model property is mapped to for filtering and cell display. These are behavioural
/// categories rather than one-to-one CLR types: all integral types map to <see cref="Int"/>, all
/// floating-point/decimal types to <see cref="Decimal"/>, and every point-in-time type
/// (DateTime, DateTimeOffset, DateOnly, TimeOnly) to <see cref="DateTime"/>. Per-type display
/// formatting is resolved from the runtime value, and the concrete CLR type is used when the filter
/// value is converted.
/// </summary>
public enum MaviGridPropertyType
{
    Undefined,
    String,
    Char,
    Bool,
    DateTime,
    TimeSpan,
    Int,
    Decimal,
    Guid,
    Enum,
    NavString,
    NavChar,
    NavBool,
    NavDateTime,
    NavTimeSpan,
    NavInt,
    NavDecimal,
    NavGuid,
    NavEnum
}
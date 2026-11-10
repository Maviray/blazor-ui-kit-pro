namespace Maviray.Blazor.Components.Pro.Core.Enums.Grid;

/// <summary>
/// The kind of value a column holds, which decides how its cells are formatted and how its filter text is read.
/// All integral types are <see cref="Int"/>, all floating-point and decimal types are <see cref="Decimal"/>, and
/// <c>DateTime</c>, <c>DateTimeOffset</c>, <c>DateOnly</c> and <c>TimeOnly</c> are <see cref="DateTime"/>.
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
    Enum
}

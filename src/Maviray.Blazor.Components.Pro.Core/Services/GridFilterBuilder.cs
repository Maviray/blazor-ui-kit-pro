using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>
/// Builds the predicate for one column filter. The filter value is read for the property's type and carried in a
/// <see cref="StrongBox{T}"/>, so a relational provider sends it as a query parameter and reuses one cached query
/// plan for every value.
/// </summary>
internal static class GridFilterBuilder<TDataItem> where TDataItem : class
{
    private static readonly System.Reflection.MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    public static Expression<Func<TDataItem, bool>> Build(string path, MaviGridFilterCriteria criteria, string value, object? nullDefault)
    {
        var parameter = Expression.Parameter(typeof(TDataItem), "x");
        var property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, path, nullDefault);

        Expression body = criteria switch
        {
            MaviGridFilterCriteria.Equals => Expression.Equal(property, Value(property.Type, value)),
            MaviGridFilterCriteria.NotEquals => Expression.NotEqual(property, Value(property.Type, value)),
            MaviGridFilterCriteria.LessThan or MaviGridFilterCriteria.Before => Expression.LessThan(property, Value(property.Type, value)),
            MaviGridFilterCriteria.LessThanOrEqual => Expression.LessThanOrEqual(property, Value(property.Type, value)),
            MaviGridFilterCriteria.GreaterThan or MaviGridFilterCriteria.After => Expression.GreaterThan(property, Value(property.Type, value)),
            MaviGridFilterCriteria.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, Value(property.Type, value)),
            MaviGridFilterCriteria.Contains => Contains(property, value),
            MaviGridFilterCriteria.Excludes => Expression.Not(Contains(property, value)),
            MaviGridFilterCriteria.IsTrue => Expression.Equal(property, Expression.Constant(true, property.Type)),
            MaviGridFilterCriteria.IsFalse => Expression.Equal(property, Expression.Constant(false, property.Type)),
            _ => throw new ArgumentOutOfRangeException(nameof(criteria), criteria, "The filter criteria isn't supported.")
        };

        return Expression.Lambda<Func<TDataItem, bool>>(body, parameter);
    }

    // x.Prop != null && x.Prop.Contains(value)
    private static Expression Contains(Expression property, string value)
    {
        if (property.Type != typeof(string))
        {
            throw new ArgumentException($"'{MaviGridFilterCriteria.Contains}' needs a string property, not '{property.Type.Name}'.");
        }

        return Expression.AndAlso(
            Expression.NotEqual(property, Expression.Constant(null, typeof(string))),
            Expression.Call(property, StringContains, Value(typeof(string), value)));
    }

    private static Expression Value(Type type, string text) =>
        Expression.Convert(Expression.Field(Expression.Constant(new StrongBox<object?>(Coerce(type, text))), nameof(StrongBox<object>.Value)), type);

    private static object Coerce(Type type, string text)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;

        if (target == typeof(string))
        {
            return text;
        }

        var trimmed = text.Trim();

        try
        {
            return target switch
            {
                _ when target.IsEnum => Enum.Parse(target, trimmed, ignoreCase: true),
                _ when target == typeof(Guid) => Guid.Parse(trimmed),
                _ when target == typeof(DateTime) => DateTime.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None),
                _ when target == typeof(DateTimeOffset) => DateTimeOffset.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None),
                _ when target == typeof(DateOnly) => DateOnly.Parse(trimmed, CultureInfo.InvariantCulture),
                _ when target == typeof(TimeOnly) => TimeOnly.Parse(trimmed, CultureInfo.InvariantCulture),
                _ when target == typeof(TimeSpan) => TimeSpan.Parse(trimmed, CultureInfo.InvariantCulture),
                _ when target == typeof(char) && trimmed.Length == 1 => trimmed[0],
                _ => Convert.ChangeType(trimmed, target, CultureInfo.InvariantCulture)
            };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidCastException or OverflowException)
        {
            throw new FormatException($"'{text}' isn't a valid {target.Name} value.", ex);
        }
    }
}

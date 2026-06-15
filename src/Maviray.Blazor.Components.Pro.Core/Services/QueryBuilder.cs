using Maviray.Blazor.Components.Pro.Core.Constants;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using System.Globalization;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Services;

public class QueryBuilder<TDataItem, TKey> where TDataItem : class where TKey : IEquatable<TKey>
{
    public static Expression<Func<TDataItem, bool>> BuildFilterExpression(string propertyPath, MaviGridFilterCriteria gridFilterCriteria, object value)
    {
        var parameter = Expression.Parameter(typeof(TDataItem), QueryConstants.PARAMETER);

        // Handle nested properties
        var property = GetPropertyExpression(parameter, propertyPath);

        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyPath}' not found.");
        }

        // Build condition based on the filter operator
        var condition = gridFilterCriteria switch
        {
            MaviGridFilterCriteria.Equals => ValueExpression(property, value, gridFilterCriteria),
            MaviGridFilterCriteria.NotEquals => ValueExpression(property, value, gridFilterCriteria),
            MaviGridFilterCriteria.LessThan => ValueExpression(property, value, gridFilterCriteria),
            MaviGridFilterCriteria.LessThanOrEqual => ValueExpression(property, value, gridFilterCriteria),
            MaviGridFilterCriteria.GreaterThan => ValueExpression(property, value, gridFilterCriteria),
            MaviGridFilterCriteria.GreaterThanOrEqual => ValueExpression(property, value, gridFilterCriteria),
            MaviGridFilterCriteria.Before => ValueExpression(property, value, gridFilterCriteria), // DateTime comparison
            MaviGridFilterCriteria.After => ValueExpression(property, value, gridFilterCriteria),

            MaviGridFilterCriteria.Contains => BuildStringMethodExpression(property, QueryConstants.CONTAINS, value),
            MaviGridFilterCriteria.Excludes => Expression.Not(BuildStringMethodExpression(property, QueryConstants.CONTAINS, value)),
            MaviGridFilterCriteria.IsTrue => Expression.Equal(property, Expression.Constant(true)),
            MaviGridFilterCriteria.IsFalse => Expression.Equal(property, Expression.Constant(false)),
            MaviGridFilterCriteria.None => throw new NotImplementedException(),
            _ => throw new NotSupportedException($"GridFilterCriteria '{gridFilterCriteria}' is not supported")
        };

        return Expression.Lambda<Func<TDataItem, bool>>(condition, parameter);
    }

    private static Expression ValueExpression(Expression property, object value, MaviGridFilterCriteria gridFilterCriteria)
    {
        var (propertyType, valueExpression) = PrepareValueExpression(property, value);

        var condition = gridFilterCriteria switch
        {
            MaviGridFilterCriteria.Equals => Expression.Equal(property, valueExpression),
            MaviGridFilterCriteria.NotEquals => Expression.NotEqual(property, valueExpression),
            MaviGridFilterCriteria.LessThan => Expression.LessThan(property, valueExpression),
            MaviGridFilterCriteria.LessThanOrEqual => Expression.LessThanOrEqual(property, valueExpression),
            MaviGridFilterCriteria.GreaterThan => Expression.GreaterThan(property, valueExpression),
            MaviGridFilterCriteria.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, valueExpression),
            MaviGridFilterCriteria.Before => Expression.LessThan(property, valueExpression), // DateTime comparison
            MaviGridFilterCriteria.After => Expression.GreaterThan(property, valueExpression),
            MaviGridFilterCriteria.Contains => BuildStringMethodExpression(property, QueryConstants.CONTAINS, value),
            MaviGridFilterCriteria.Excludes => Expression.Not(BuildStringMethodExpression(property, QueryConstants.CONTAINS, value)),
            MaviGridFilterCriteria.IsTrue => Expression.Equal(property, Expression.Constant(true)),
            MaviGridFilterCriteria.IsFalse => Expression.Equal(property, Expression.Constant(false)),

            _ => throw new NotSupportedException($"GridFilterCriteria '{gridFilterCriteria}' is not supported")
        };

        return condition;
    }

    private static Expression? GetPropertyExpression(Expression parameter, string propertyPath)
    {
        return propertyPath.Split('.').Aggregate(parameter, Expression.PropertyOrField);
    }

    private static (Type propertyType, Expression valueExpression) PrepareValueExpression(Expression property, object value)
    {
        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

        // The comparison constant is always built as the enum CLR type, so EF Core translates it
        // according to the column's value converter - a numeric column compares against the
        // underlying number, a string column compares against the member name. We just have to
        // coerce the incoming filter value (which may be a member name, a numeric string, or a
        // boxed numeric value) into the matching enum member first.
        var coercedValue = CoerceValue(propertyType, value);

        var convertedValue = Expression.Constant(coercedValue, property.Type);
        return (propertyType, convertedValue);
    }

    private static object CoerceValue(Type targetType, object value)
    {
        if (targetType.IsEnum)
        {
            return ConvertToEnum(targetType, value);
        }

        // Already the target type (e.g. a Guid or DateTime passed in strongly typed).
        if (value.GetType() == targetType)
        {
            return value;
        }

        // Types that are not IConvertible and therefore unsupported by Convert.ChangeType. They are
        // parsed explicitly from their text form using the invariant culture.
        if (value is string text)
        {
            text = text.Trim();

            if (targetType == typeof(Guid))
            {
                return Guid.Parse(text);
            }

            if (targetType == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None);
            }

            if (targetType == typeof(DateOnly))
            {
                return DateOnly.Parse(text, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(TimeOnly))
            {
                return TimeOnly.Parse(text, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(TimeSpan))
            {
                return TimeSpan.Parse(text, CultureInfo.InvariantCulture);
            }
        }

        // Everything else (numeric types, bool, char, string, DateTime) is IConvertible.
        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static object ConvertToEnum(Type enumType, object value)
    {
        // Already the right enum (e.g. passed through strongly typed).
        if (value.GetType() == enumType)
        {
            return value;
        }

        // A string can be either the member name ("Active") or the numeric value ("2");
        // Enum.Parse handles both, and ignoreCase makes name matching forgiving.
        if (value is string text)
        {
            return Enum.Parse(enumType, text.Trim(), ignoreCase: true);
        }

        // Any other value is treated as the enum's underlying numeric value (int, long, byte, ...),
        // which is the form a number-backed column hands back. Enum.ToObject is the reliable path
        // here - Convert.ChangeType does not convert numbers to enum types.
        var underlyingType = Enum.GetUnderlyingType(enumType);
        var numericValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        return Enum.ToObject(enumType, numericValue);
    }

    private static Expression BuildStringMethodExpression(Expression property, string methodName, object value)
    {
        if (property.Type != typeof(string))
        {
            throw new InvalidOperationException($"'{methodName}' can only be used on string properties.");
        }

        var method = typeof(string).GetMethod(methodName, [typeof(string)]);

        return method == null ? throw new InvalidOperationException("attempt to construct MethodInfo resulted in null.") : Expression.Call(property, method, Expression.Constant(value));
    }

    /// <summary>
    /// Combines two lambda expressions with an AndAlso or OrElse condition.
    /// </summary>
    public static Expression<Func<TDataItem, bool>> CombineExpressions(Expression<Func<TDataItem, bool>> expr1, Expression<Func<TDataItem, bool>> expr2, MaviCombineExpressionCriteria combineExpressionCriteria)
    {
        // Create a parameter for the combined expression
        var parameter = Expression.Parameter(typeof(TDataItem), QueryConstants.PARAMETER);

        // Replace parameters in the first expression
        var leftVisitor = new MaviExpressionVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body);

        // Replace parameters in the second expression
        var rightVisitor = new MaviExpressionVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body);

        // Combine the two expressions using AND
        if (combineExpressionCriteria == MaviCombineExpressionCriteria.AndAlso)
        {
            var combined = Expression.AndAlso(left, right);
            return Expression.Lambda<Func<TDataItem, bool>>(combined, parameter);
        }
        else
        {
            var combined = Expression.OrElse(left, right);
            return Expression.Lambda<Func<TDataItem, bool>>(combined, parameter);
        }
    }
}
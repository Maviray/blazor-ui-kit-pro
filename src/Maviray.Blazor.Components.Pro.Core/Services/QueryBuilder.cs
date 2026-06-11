using Maviray.Blazor.Components.Pro.Core.Constants;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
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

        // Handle enum parsing
        if (propertyType.IsEnum && value is string enumValue)
        {
            value = Enum.Parse(propertyType, enumValue);
        }

        // Ensure value is correctly converted
        var convertedValue = Expression.Constant(Convert.ChangeType(value, propertyType), property.Type);
        return (propertyType, convertedValue);
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
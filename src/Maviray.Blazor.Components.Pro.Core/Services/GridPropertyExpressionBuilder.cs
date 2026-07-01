using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>
/// Builds the property-access expression shared by <see cref="Extensions.QueryableExtensions.OrderByDynamic{TEntity}"/>
/// and <see cref="QueryBuilder{TDataItem,TKey}.BuildFilterExpression"/>: walks a dotted property path off
/// <paramref name="parameter"/>, then - only when <paramref name="nullDefault"/> is supplied - coalesces a
/// <see langword="null"/> at that path to the given default, so a nullable navigation value sorts/filters
/// at the data source the same way a model's <c>ConstructFrom</c> coalesces it in memory (e.g. <c>?? false</c>).
/// </summary>
internal static class GridPropertyExpressionBuilder
{
    public static Expression BuildPropertyExpression(Expression parameter, string propertyPath, object? nullDefault)
    {
        Expression property = propertyPath.Split('.').Aggregate(parameter, Expression.PropertyOrField);

        if (nullDefault == null)
        {
            return property;
        }

        var underlyingType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
        return Expression.Coalesce(property, Expression.Constant(nullDefault, underlyingType));
    }
}

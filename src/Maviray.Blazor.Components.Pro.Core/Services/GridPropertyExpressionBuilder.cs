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

        // Expression.Coalesce requires its left operand to be a reference type or Nullable<T>. A
        // property reached through an optional navigation (e.g. Customer.Region?.Rating, where
        // Region is optional but Rating is a plain non-nullable enum declared on Region itself)
        // has a non-nullable static type even though the value can be missing at the database
        // level - convert to Nullable<T> first so Coalesce is valid to construct.
        Expression coalesceSource = property.Type.IsValueType && Nullable.GetUnderlyingType(property.Type) == null
            ? Expression.Convert(property, typeof(Nullable<>).MakeGenericType(property.Type))
            : property;

        return Expression.Coalesce(coalesceSource, Expression.Constant(nullDefault, underlyingType));
    }
}

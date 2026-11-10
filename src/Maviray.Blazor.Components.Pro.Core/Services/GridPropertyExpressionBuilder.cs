using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>
/// Builds the member access for a dotted path off <paramref name="parameter"/>, shared by sorting and filtering.
/// When a null default is given, a <see langword="null"/> at the path is coalesced to it, so a nullable value sorts
/// and filters at the data source the way <c>ConstructFrom</c> coalesces it in memory (for example <c>?? false</c>).
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

        // Coalesce needs a reference type or Nullable<T> on the left. A value reached through an optional navigation
        // (Customer.Region.Rating with Region optional) has a non-nullable static type but can still be missing in the
        // database, so lift it to Nullable<T> first.
        var coalesceSource = property.Type.IsValueType && Nullable.GetUnderlyingType(property.Type) == null
            ? Expression.Convert(property, typeof(Nullable<>).MakeGenericType(property.Type))
            : property;

        return Expression.Coalesce(coalesceSource, Expression.Constant(nullDefault, underlyingType));
    }
}

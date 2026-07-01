using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Constants;
using Maviray.Blazor.Components.Pro.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Services;
using System.Linq.Expressions;
using System.Reflection;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Builds a dynamic OrderBy or OrderByDescending query against a property whose type is unknown at compile time.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="source">The IQueryable source</param>
    /// <param name="propertyName">The name of the property, supports nested properties like "Property.SubProperty"</param>
    /// <param name="sortOrder">indicates if order should be ascending or descending</param>
    /// <param name="nullDefault">Optional default value to coalesce nulls to at the data source; when supplied, null values sort the same as this default</param>
    /// <returns>A dynamically ordered IQueryable</returns>
    public static IQueryable<TEntity> OrderByDynamic<TEntity>(this IQueryable<TEntity> source, string propertyName, SortOrder sortOrder, object? nullDefault = null)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        }

        // Parameter for the lambda: x => x.Property
        var parameter = Expression.Parameter(typeof(TEntity), QueryConstants.PARAMETER);

        // Support nested properties; when nullDefault is supplied the final access is coalesced so a
        // NULL at the data source sorts the same as the in-memory default the model's ConstructFrom uses.
        Expression property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, propertyName, nullDefault);

        // Get the property type dynamically
        var propertyType = property.Type;

        // Build the lambda expression: x => x.Property
        var lambda = Expression.Lambda(property, parameter);

        // Determine the OrderBy or OrderByDescending method
        var methodName = sortOrder == SortOrder.Ascending ? QueryConstants.ORDER_BY : QueryConstants.ORDER_BY_DESCENDING;

        // Find the generic OrderBy method
        var method = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), propertyType);

        // Apply the method dynamically to the source query
        var invokedMethod = method.Invoke(null, [source, lambda]);

        if (invokedMethod == null)
        {
            return source;
        }

        var resultingQuery = (IQueryable<TEntity>)invokedMethod;

        return resultingQuery;
    }
}
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Services;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

/// <summary>Ordering by a dot-path known only at run time, for <see cref="Interfaces.Grid.IGridDataSource{TDataItem}"/> implementations.</summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Orders by <paramref name="propertyPath"/> (for example <c>"Customer.Name"</c>). When
    /// <paramref name="nullDefault"/> is set, a <see langword="null"/> at the path sorts as that value.
    /// </summary>
    public static IOrderedQueryable<T> OrderByDynamic<T>(this IQueryable<T> source, string propertyPath, SortOrder sortOrder, object? nullDefault = null) =>
        Order(source, propertyPath, nullDefault, sortOrder == SortOrder.Ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending));

    /// <summary>
    /// Adds <paramref name="propertyPath"/> as a further sort key, for example a unique key that breaks ties. When
    /// <paramref name="nullDefault"/> is set, a <see langword="null"/> at the path sorts as that value.
    /// </summary>
    public static IOrderedQueryable<T> ThenByDynamic<T>(this IOrderedQueryable<T> source, string propertyPath, SortOrder sortOrder, object? nullDefault = null) =>
        Order(source, propertyPath, nullDefault, sortOrder == SortOrder.Ascending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending));

    /// <summary>
    /// Loads new <typeparamref name="T"/> instances holding only the members at <paramref name="paths"/> (dot-paths;
    /// a path through a related item loads just that member of it, and a missing related item stays
    /// <see langword="null"/>). With EF Core this selects only those columns. Apply it last, after ordering and paging.
    /// </summary>
    /// <exception cref="ArgumentException">A path doesn't name public members.</exception>
    /// <exception cref="InvalidOperationException">A type on a path has no parameterless constructor, or a member can't be set.</exception>
    public static IQueryable<T> SelectPaths<T>(this IQueryable<T> source, IEnumerable<string> paths) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(GridProjectionBuilder<T>.For(paths));
    }

    private static IOrderedQueryable<T> Order<T>(IQueryable<T> source, string propertyPath, object? nullDefault, string method)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyPath);

        var parameter = Expression.Parameter(typeof(T), "x");
        var key = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, propertyPath, nullDefault);
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), key.Type], source.Expression, Expression.Quote(Expression.Lambda(key, parameter)));

        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }
}

using System.Linq.Expressions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Interfaces;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Services;

/// <summary>
/// Global grid filter that scopes every <see cref="IScopedItem"/> entity to the current user's
/// permitted scopes and auth areas. Registered once in DI, it is applied by the data factory to any
/// grid whose entity implements <see cref="IScopedItem"/> - no per-page wiring required. Entities
/// that do not implement the interface are left untouched.
/// </summary>
public class ScopedItemGridFilter : IGridGlobalFilterProvider
{
    private readonly IUserScopeContext _userScope;

    public ScopedItemGridFilter(IUserScopeContext userScope)
    {
        _userScope = userScope;
    }

    public Expression<Func<TDataItem, bool>>? GetFilter<TDataItem>() where TDataItem : class
    {
        // Only constrain entities that opt into scoping; everything else is unaffected.
        if (!typeof(IScopedItem).IsAssignableFrom(typeof(TDataItem)))
        {
            return null;
        }

        // Use nullable lists so the predicate is "ScopeId IN (...)" - directly translatable by EF and
        // null-safe (rows with a null ScopeId/AuthAreaId are naturally excluded).
        var allowedScopeIds = _userScope.AllowedScopeIds.Select(id => (int?)id).ToList();
        var allowedAuthAreaIds = _userScope.AllowedAuthAreaIds.Select(id => (int?)id).ToList();

        var parameter = Expression.Parameter(typeof(TDataItem), "x");

        var scopeMatches = BuildContains(parameter, nameof(IScopedItem.ScopeId), allowedScopeIds);
        var authAreaMatches = BuildContains(parameter, nameof(IScopedItem.AuthAreaId), allowedAuthAreaIds);

        var body = Expression.AndAlso(scopeMatches, authAreaMatches);
        return Expression.Lambda<Func<TDataItem, bool>>(body, parameter);
    }

    // Builds: allowedIds.Contains(x.<propertyName>)
    private static Expression BuildContains(ParameterExpression parameter, string propertyName, List<int?> allowedIds)
    {
        var property = Expression.Property(parameter, propertyName);
        var containsMethod = typeof(List<int?>).GetMethod(nameof(List<int?>.Contains), [typeof(int?)])!;
        return Expression.Call(Expression.Constant(allowedIds), containsMethod, property);
    }
}

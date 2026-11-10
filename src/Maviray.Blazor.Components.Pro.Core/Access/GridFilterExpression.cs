using System.Linq.Expressions;
using System.Reflection;

namespace Maviray.Blazor.Components.Pro.Core.Access;

/// <summary>
/// Turns a row filter written against an interface that entities implement (such as <c>ITenantScoped</c>) into a
/// filter on the entity's own properties, which a query provider such as EF Core can translate.
/// </summary>
public static class GridFilterExpression
{
    public static Expression<Func<TDataItem, bool>> ForInterface<TDataItem, TInterface>(Expression<Func<TInterface, bool>> filter)
        where TDataItem : class, TInterface =>
        (Expression<Func<TDataItem, bool>>)ForInterface(typeof(TDataItem), filter);

    /// <summary>
    /// Rebinds <paramref name="filter"/> (one parameter of an interface type, returning <see cref="bool"/>) to
    /// <paramref name="dataItemType"/>. The result is an <c>Expression&lt;Func&lt;dataItemType, bool&gt;&gt;</c>; inside an
    /// open-generic policy, cast it to <c>Expression&lt;Func&lt;TDataItem, bool&gt;&gt;</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The filter isn't a predicate over an interface the type implements.</exception>
    /// <exception cref="InvalidOperationException">An interface member the filter uses is implemented explicitly.</exception>
    public static LambdaExpression ForInterface(Type dataItemType, LambdaExpression filter)
    {
        ArgumentNullException.ThrowIfNull(dataItemType);
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.Parameters.Count != 1 || filter.ReturnType != typeof(bool))
        {
            throw new ArgumentException("The filter must take one parameter and return bool.", nameof(filter));
        }

        var interfaceParameter = filter.Parameters[0];

        if (!interfaceParameter.Type.IsInterface || !interfaceParameter.Type.IsAssignableFrom(dataItemType))
        {
            throw new ArgumentException($"'{dataItemType.Name}' doesn't implement '{interfaceParameter.Type.Name}'.", nameof(filter));
        }

        var dataItemParameter = Expression.Parameter(dataItemType, interfaceParameter.Name);
        var body = new InterfaceRebinder(interfaceParameter, dataItemParameter).Visit(filter.Body);

        return Expression.Lambda(typeof(Func<,>).MakeGenericType(dataItemType, typeof(bool)), body, dataItemParameter);
    }

    private sealed class InterfaceRebinder(ParameterExpression interfaceParameter, ParameterExpression dataItemParameter) : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == interfaceParameter && node.Member is PropertyInfo { DeclaringType.IsInterface: true } interfaceProperty)
            {
                return Expression.Property(dataItemParameter, FindImplementation(interfaceProperty));
            }

            return base.VisitMember(node);
        }

        // Any other use of the parameter (a method call, a comparison) keeps working through the interface.
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == interfaceParameter ? Expression.Convert(dataItemParameter, interfaceParameter.Type) : base.VisitParameter(node);

        private PropertyInfo FindImplementation(PropertyInfo interfaceProperty)
        {
            var dataItemType = dataItemParameter.Type;
            var interfaceGetter = interfaceProperty.GetMethod
                ?? throw new InvalidOperationException($"'{interfaceProperty.DeclaringType!.Name}.{interfaceProperty.Name}' has no getter.");

            var map = dataItemType.GetInterfaceMap(interfaceProperty.DeclaringType!);
            var targetGetter = map.TargetMethods[Array.IndexOf(map.InterfaceMethods, interfaceGetter)];

            var implementation = targetGetter.IsPublic
                ? dataItemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => p.GetMethod?.MethodHandle == targetGetter.MethodHandle)
                : null;

            return implementation ?? throw new InvalidOperationException(
                $"'{interfaceProperty.DeclaringType!.Name}.{interfaceProperty.Name}' is implemented explicitly by '{dataItemType.Name}'; " +
                "implement it as a public property so the filter can be translated.");
        }
    }
}

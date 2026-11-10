using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Services;

internal static class PredicateCombiner
{
    /// <summary>AND-combines the predicates into one lambda over a single parameter, or returns <see langword="null"/> for none.</summary>
    public static Expression<Func<T, bool>>? AndAll<T>(IReadOnlyList<Expression<Func<T, bool>>> predicates)
    {
        switch (predicates.Count)
        {
            case 0:
                return null;
            case 1:
                return predicates[0];
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? body = null;

        foreach (var predicate in predicates)
        {
            var rebound = new ParameterReplacer(predicate.Parameters[0], parameter).Visit(predicate.Body);
            body = body is null ? rebound : Expression.AndAlso(body, rebound);
        }

        return Expression.Lambda<Func<T, bool>>(body!, parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}

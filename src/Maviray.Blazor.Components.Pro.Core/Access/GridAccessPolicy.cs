using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Access;

/// <summary>
/// Base class for a policy about one entity type and every type derived from it (or, for an interface, every type
/// that implements it). It is <see cref="GridAccessOutcome.NotApplicable"/> for every other type and lets the derived
/// policy work with typed decisions and row filters.
/// </summary>
/// <remarks>
/// A grid over a derived type (EF Core inheritance) gets the same decision, with the row filter rebound to the
/// derived type, so a policy can't be bypassed by querying a subtype. A policy for the derived type doesn't cover its
/// base type.
/// </remarks>
public abstract class GridAccessPolicy<TEntity> : IGridAccessPolicy where TEntity : class
{
    /// <summary>Decides access to <typeparamref name="TEntity"/> and its subtypes.</summary>
    /// <remarks><see cref="GridAccessRequest.DataItemType"/> is the grid's actual data item type.</remarks>
    protected abstract ValueTask<GridAccessDecision<TEntity>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken);

    ValueTask<GridAccessDecision<TDataItem>> IGridAccessPolicy.EvaluateAsync<TDataItem>(GridAccessRequest request, CancellationToken cancellationToken)
    {
        if (!typeof(TEntity).IsAssignableFrom(typeof(TDataItem)))
        {
            return ValueTask.FromResult(GridAccessDecision<TDataItem>.NotApplicable());
        }

        var pending = EvaluateAsync(request, cancellationToken);

        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(As<TDataItem>(pending.Result))
            : AwaitAsync<TDataItem>(pending);
    }

    private static async ValueTask<GridAccessDecision<TDataItem>> AwaitAsync<TDataItem>(ValueTask<GridAccessDecision<TEntity>> pending)
        where TDataItem : class =>
        As<TDataItem>(await pending.ConfigureAwait(false));

    private static GridAccessDecision<TDataItem> As<TDataItem>(GridAccessDecision<TEntity> decision) where TDataItem : class
    {
        // For TEntity itself the cast only restates the type; a missing decision is passed on for the factory to report.
        if (typeof(TDataItem) == typeof(TEntity) || decision is null)
        {
            return (GridAccessDecision<TDataItem>)(object)decision!;
        }

        return decision.Rebind(decision.RowFilter is null
            ? null
            : (Expression<Func<TDataItem, bool>>)GridFilterExpression.ForSubtype(typeof(TDataItem), decision.RowFilter));
    }
}

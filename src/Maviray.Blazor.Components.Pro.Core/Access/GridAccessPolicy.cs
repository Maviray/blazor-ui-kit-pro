namespace Maviray.Blazor.Components.Pro.Core.Access;

/// <summary>
/// Base class for a policy about one entity type. It is <see cref="GridAccessOutcome.NotApplicable"/> for every other
/// type and lets the derived policy work with typed decisions and row filters.
/// </summary>
public abstract class GridAccessPolicy<TEntity> : IGridAccessPolicy where TEntity : class
{
    /// <summary>Decides access to <typeparamref name="TEntity"/>.</summary>
    protected abstract ValueTask<GridAccessDecision<TEntity>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken);

    ValueTask<GridAccessDecision<TDataItem>> IGridAccessPolicy.EvaluateAsync<TDataItem>(GridAccessRequest request, CancellationToken cancellationToken)
    {
        if (typeof(TDataItem) != typeof(TEntity))
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

    // TDataItem is TEntity here; the cast only restates that for the compiler.
    private static GridAccessDecision<TDataItem> As<TDataItem>(GridAccessDecision<TEntity> decision) where TDataItem : class =>
        (GridAccessDecision<TDataItem>)(object)decision;
}

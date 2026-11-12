namespace Maviray.Blazor.Components.Pro.Core.Access;

/// <summary>
/// Decides, before every grid fetch, whether the current user may read a data item type and which rows. Policies
/// run in registration order: the first <see cref="GridAccessOutcome.Denied"/> ends the evaluation and denies the
/// fetch; the row filters of every <see cref="GridAccessOutcome.Allowed"/> are AND-combined and apply to the count
/// and the page. An exception or a missing decision fails the fetch; it never counts as allowed.
/// </summary>
/// <remarks>
/// One registration serves every data item type: return <see cref="GridAccessDecision{TDataItem}.NotApplicable"/>
/// for types the policy doesn't govern. For a policy about a single type, derive from
/// <see cref="GridAccessPolicy{TEntity}"/>. Policies resolve from the factory's DI scope (the circuit in Blazor Server).
/// </remarks>
public interface IGridAccessPolicy
{
    ValueTask<GridAccessDecision<TDataItem>> EvaluateAsync<TDataItem>(GridAccessRequest request, CancellationToken cancellationToken)
        where TDataItem : class;
}

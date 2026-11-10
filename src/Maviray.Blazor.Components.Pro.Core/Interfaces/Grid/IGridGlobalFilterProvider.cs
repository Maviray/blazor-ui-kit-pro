using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>
/// A row constraint applied to every grid of every data item type, such as hiding soft-deleted rows. It is
/// AND-combined with the other filters and applies to the count and the page.
/// </summary>
/// <remarks>
/// A global filter only narrows rows; it doesn't grant or deny access. For decisions that need the current user, an
/// asynchronous lookup, or the power to deny a fetch, use an access policy.
/// </remarks>
public interface IGridGlobalFilterProvider
{
    /// <summary>
    /// Returns the constraint for <typeparamref name="TDataItem"/>, or <see langword="null"/> when this provider
    /// doesn't constrain that type (typically checked through a marker interface).
    /// </summary>
    Expression<Func<TDataItem, bool>>? GetFilter<TDataItem>() where TDataItem : class;
}

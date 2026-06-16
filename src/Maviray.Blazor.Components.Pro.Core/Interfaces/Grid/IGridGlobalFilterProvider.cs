using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>
/// Supplies a cross-cutting filter that the data factory applies to every grid query, regardless of
/// the concrete data item type. Register an implementation in DI (see the AddGridGlobalFilter
/// extensions) to enforce conditions such as soft-delete exclusion or tenant/permission scoping in a
/// single place instead of on every page. The returned predicate is AND-combined with the grid's own
/// filter and any per-call filter, and applies to both the row count and the returned page.
/// </summary>
public interface IGridGlobalFilterProvider
{
    /// <summary>
    /// Returns the filter to apply for the given data item type, or <see langword="null"/> when this
    /// provider imposes no constraint on that type. Implementations typically test whether
    /// <typeparamref name="TDataItem"/> implements a marker interface (e.g. a soft-delete or
    /// tenant-scoped contract) and build the predicate accordingly.
    /// </summary>
    Expression<Func<TDataItem, bool>>? GetFilter<TDataItem>() where TDataItem : class;
}

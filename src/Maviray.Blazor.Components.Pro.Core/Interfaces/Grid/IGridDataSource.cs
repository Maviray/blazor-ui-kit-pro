using Maviray.Blazor.Components.Core.Enums;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

public interface IGridDataSource<TDataItem> : IAsyncDisposable where TDataItem : class
{
    Task<int> Count(Expression<Func<TDataItem, bool>>? filter = null);

    /// <param name="includePaths">
    /// Names of navigation properties on <typeparamref name="TDataItem"/> to eager-load so that the
    /// related data is available when projecting nested grid columns. Paths may be dotted for deeper
    /// navigations (e.g. "Order.Customer").
    /// </param>
    /// <param name="includeConfigurator">
    /// An optional, strongly-typed query shaper applied to the root query before filtering, ordering and
    /// paging. It is intended for eager-loading related entities (e.g. EF Core
    /// <c>Include(...).ThenInclude(...)</c>) that cannot be expressed as the dotted string paths in
    /// <paramref name="includePaths"/>, so the materialized graph is available when each model is built.
    /// When both are supplied the configurator is applied in addition to <paramref name="includePaths"/>.
    /// </param>
    /// <param name="orderByNullDefault">
    /// When supplied, a <see langword="null"/> value at <paramref name="orderBy"/>'s resolved path is
    /// coalesced to this default before ordering, so sorting matches whatever default the model's
    /// <c>ConstructFrom</c> applies in memory (e.g. <c>?? false</c>).
    /// </param>
    Task<IQueryable<TDataItem>?> Get(SortOrder sortOrder, string orderBy, int skip, int numberOfItemsToTake, Expression<Func<TDataItem, bool>>? filter = null, IReadOnlyCollection<string>? includePaths = null, GridIncludeConfigurator<TDataItem>? includeConfigurator = null, object? orderByNullDefault = null);
}
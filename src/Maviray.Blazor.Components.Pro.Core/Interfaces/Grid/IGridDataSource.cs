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
    Task<IQueryable<TDataItem>?> Get(SortOrder sortOrder, string orderBy, int skip, int numberOfItemsToTake, Expression<Func<TDataItem, bool>>? filter = null, IReadOnlyCollection<string>? includePaths = null);
}
using Maviray.Blazor.Components.Pro.Core.Enums;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

public interface IGridDataSource<TDataItem> : IAsyncDisposable where TDataItem : class
{
    Task<int> Count(Expression<Func<TDataItem, bool>>? filter = null);

    Task<IQueryable<TDataItem>?> Get(MaviSortOrder sortOrder, string orderBy, int skip, int numberOfItemsToTake, Expression<Func<TDataItem, bool>>? filter = null);
}
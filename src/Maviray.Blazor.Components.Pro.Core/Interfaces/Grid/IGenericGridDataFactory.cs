using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

public interface IGenericGridDataFactory<TDataItem, TModel, TKey> : IAsyncDisposable where TDataItem : class where TModel : class, IGridCollectionModel<TDataItem, TKey> where TKey : IEquatable<TKey>
{
    /// <param name="additionalFilter">
    /// An optional consumer-supplied predicate (for example a permission or data-scope constraint)
    /// that is AND-combined with the filter derived from <paramref name="queryParameters"/>. When
    /// supplied it limits both the total count and the returned page to the matching data.
    /// </param>
    Task<MaviGridCollection<TKey>?> BuildDataCollection(MaviGridDataQueryParameters queryParameters, Expression<Func<TDataItem, bool>>? additionalFilter = null);
}
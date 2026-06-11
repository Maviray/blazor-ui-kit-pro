using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

public interface IGenericGridDataFactory<TDataItem, TModel, TKey> : IAsyncDisposable where TDataItem : class where TModel : class, IGridCollectionModel<TDataItem, TKey> where TKey : IEquatable<TKey>
{
    Task<MaviGridCollection<TKey>> BuildDataCollection(MaviGridDataQueryParameters queryParameters);
}
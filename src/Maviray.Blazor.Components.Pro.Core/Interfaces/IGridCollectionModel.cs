using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces;

public interface IGridCollectionModel<in TDataItem, TKey> where TDataItem : class where TKey : IEquatable<TKey>
{
    TKey Key { get; }
    void ConstructFrom(TDataItem dataItem);

    IEnumerable<IMaviRowContextMenuItem<TKey>> GetContextActions();
}
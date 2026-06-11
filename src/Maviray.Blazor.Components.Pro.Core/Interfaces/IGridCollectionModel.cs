namespace Maviray.Blazor.Components.Pro.Core.Interfaces;

public interface IGridCollectionModel<in TDataItem, out TKey> where TDataItem : class where TKey : IEquatable<TKey>
{
    TKey Key { get; }
    void ConstructFrom(TDataItem dataItem);
}
using Maviray.Blazor.Components.Pro.Core.Models.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>
/// Builds one grid page. Each fetch runs the access policies, combines every filter, counts the matching rows,
/// loads only the rows of the requested page, and formats them into cells.
/// </summary>
public interface IGridDataFactory<TDataItem, TModel, TKey>
    where TDataItem : class
    where TModel : class, IGridCollectionModel<TDataItem, TKey>
    where TKey : IEquatable<TKey>
{
    /// <param name="query">The page, sort and column filters requested by the grid.</param>
    /// <param name="options">The grid's settings; reuse one instance per grid.</param>
    /// <param name="cancellationToken">Stops the fetch before or during each data source call.</param>
    Task<MaviGridCollection<TKey>> BuildPageAsync(
        MaviGridQuery query,
        GridFetchOptions<TDataItem, TModel>? options = null,
        CancellationToken cancellationToken = default);
}

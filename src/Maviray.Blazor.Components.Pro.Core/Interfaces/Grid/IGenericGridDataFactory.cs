using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

public interface IGenericGridDataFactory<TDataItem, TModel, TKey> : IAsyncDisposable where TDataItem : class where TModel : class, IGridCollectionModel<TDataItem, TKey> where TKey : IEquatable<TKey>
{
    /// <param name="queryParameters"></param>
    /// <param name="additionalFilter">
    /// An optional consumer-supplied predicate (for example a permission or data-scope constraint)
    /// that is AND-combined with the filter derived from <paramref name="queryParameters"/>. When
    /// supplied it limits both the total count and the returned page to the matching data.
    /// </param>
    /// <param name="cellThemeResolvers">
    /// Optional per-column delegates, keyed by column key, that decide the <see cref="ThemeColorScheme"/>
    /// for each cell of a column marked for graphical display (<see cref="MaviGridColumn.GraphicDisplay"/>).
    /// A resolver receives the finalised cell together with the cell's raw, unformatted property value
    /// (so it can run typed logic such as numeric threshold comparisons) and returns the scheme an
    /// advanced renderer should use. Resolvers for columns that are not graphic, or columns without a
    /// resolver, are ignored.
    /// </param>
    /// <param name="includeConfigurator">
    /// An optional, strongly-typed query shaper applied to the root query before filtering, ordering and
    /// paging. Use it to eager-load related entities — typically via EF Core
    /// <c>Include(...).ThenInclude(...)</c> — that cannot be expressed through the model's
    /// <c>[GridNavProperty]</c> string paths, so the whole graph is materialized by the same paged query
    /// and is available when each <typeparamref name="TModel"/> is built in its
    /// <see cref="IGridCollectionModel{TDataItem,TKey}.ConstructFrom"/>.
    /// </param>
    Task<MaviGridCollection<TKey>?> BuildDataCollection(
        MaviGridDataQueryParameters queryParameters,
        Expression<Func<TDataItem, bool>>? additionalFilter = null,
        IReadOnlyDictionary<string, Func<MaviGridCell<TKey>, object?, ThemeColorScheme>>? cellThemeResolvers = null,
        GridIncludeConfigurator<TDataItem>? includeConfigurator = null);
}
namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>
/// Shapes the root grid query before filtering, ordering and paging are applied. It is intended to
/// eager-load related entities — typically via EF Core <c>Include</c>/<c>ThenInclude</c> — so that the
/// whole object graph is materialized as part of the original (paged) query and is therefore available
/// inside <see cref="IGridCollectionModel{TDataItem,TKey}.ConstructFrom"/> when each model is built.
/// </summary>
/// <remarks>
/// The delegate is deliberately typed only over <see cref="IQueryable{T}"/> so the core library stays
/// provider-agnostic: EF Core's <c>Include(...).ThenInclude(...)</c> returns an
/// <c>IIncludableQueryable&lt;TEntity,TProperty&gt;</c>, which is an <see cref="IQueryable{T}"/>, so the
/// consumer can express an arbitrarily deep include tree in the EF-aware layer while this assembly takes
/// no dependency on Entity Framework. Implementations must return the same query they receive with the
/// extra shaping applied and must not enumerate it.
/// </remarks>
/// <typeparam name="TDataItem">The root entity type the grid is bound to.</typeparam>
/// <param name="query">The root query (the <c>DbSet</c>) to shape.</param>
/// <returns>The shaped query, with the requested navigations eager-loaded.</returns>
public delegate IQueryable<TDataItem> GridIncludeConfigurator<TDataItem>(IQueryable<TDataItem> query)
    where TDataItem : class;

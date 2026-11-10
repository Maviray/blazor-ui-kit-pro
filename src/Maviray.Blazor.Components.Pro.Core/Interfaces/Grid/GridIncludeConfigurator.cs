namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>
/// Shapes the root grid query before filtering, ordering and paging, typically by eager-loading related entities
/// with EF Core <c>Include(...).ThenInclude(...)</c>, so the paged query loads the whole graph that
/// <see cref="IGridCollectionModel{TDataItem,TKey}.ConstructFrom"/> reads.
/// </summary>
/// <remarks>
/// Typed over <see cref="IQueryable{T}"/> so this library stays provider-agnostic; EF Core's include results are
/// <see cref="IQueryable{T}"/>. Return the query you receive with the shaping applied, and don't enumerate it.
/// </remarks>
public delegate IQueryable<TDataItem> GridIncludeConfigurator<TDataItem>(IQueryable<TDataItem> query)
    where TDataItem : class;

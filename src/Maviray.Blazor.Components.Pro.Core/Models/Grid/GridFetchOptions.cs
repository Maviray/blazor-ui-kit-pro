using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// The settings of one grid that stay the same from page to page. Create it once per grid (for example as a field
/// of the page) and pass it to every fetch, so the factory can reuse the columns it built for it.
/// </summary>
public sealed class GridFetchOptions<TDataItem, TModel> where TDataItem : class
{
    /// <summary>A row constraint for this grid, AND-combined with everything else; it limits the count and the page.</summary>
    public Expression<Func<TDataItem, bool>>? Filter { get; init; }

    /// <summary>
    /// Shapes the root query before filtering, ordering and paging, typically EF Core
    /// <c>Include(...).ThenInclude(...)</c> so that <c>ConstructFrom</c> sees the related data.
    /// </summary>
    public GridIncludeConfigurator<TDataItem>? Include { get; init; }

    /// <summary>Data item paths for model columns whose name has no matching data item member.</summary>
    public GridPropertyPathMap<TDataItem, TModel>? PropertyPathMap { get; init; }

    /// <summary>
    /// When set, pages load only the visible columns' data item paths plus the members the projection lists, instead
    /// of whole entities with their includes. See <see cref="GridProjection{TDataItem}"/> for what to list.
    /// </summary>
    public GridProjection<TDataItem>? Projection { get; init; }

    /// <summary>
    /// Colour of each cell in a graphic column, keyed by column key. A resolver receives the cell's raw property
    /// value. Entries for columns that are not graphic are ignored.
    /// </summary>
    public IReadOnlyDictionary<string, Func<object?, ThemeColorScheme>>? CellThemes { get; init; }

    /// <summary>
    /// A tag passed to access policies and fetch observers. It has no meaning to the grid; use it to tell apart grids
    /// over the same type that need different rules.
    /// </summary>
    public string? AccessTag { get; init; }
}

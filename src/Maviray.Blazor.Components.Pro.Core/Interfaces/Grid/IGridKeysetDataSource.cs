using Maviray.Blazor.Components.Pro.Core.Models.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>
/// A data source that can also page by keyset. The factory loads pages through <see cref="GetKeysetPageAsync"/> and
/// hands the returned cursors to the grid, so moving to the next or previous page seeks from the last or first row
/// instead of skipping rows, which costs the same on every page.
/// </summary>
/// <remarks>
/// <see cref="Extensions.GridKeyset{T}"/> implements the ordering, the cursors and the seek over an
/// <see cref="IQueryable{T}"/>.
/// </remarks>
public interface IGridKeysetDataSource<TDataItem> : IGridDataSource<TDataItem> where TDataItem : class
{
    /// <summary>
    /// Loads one page like <see cref="IGridDataSource{TDataItem}.GetPageAsync"/>, or, when
    /// <see cref="GridDataSourceQuery{TDataItem}.Cursor"/> is set and usable, the rows after or before it. Returns the
    /// cursors of the first and last rows, or <see langword="null"/> cursors when the order can't be sought.
    /// </summary>
    Task<GridKeysetPage<TDataItem>> GetKeysetPageAsync(GridDataSourceQuery<TDataItem> query, CancellationToken cancellationToken = default);
}

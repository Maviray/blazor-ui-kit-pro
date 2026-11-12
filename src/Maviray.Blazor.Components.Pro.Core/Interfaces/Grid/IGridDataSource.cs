namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

/// <summary>
/// Reads grid rows from a backing store. For each fetch the factory calls <see cref="CountAsync"/> once and then
/// <see cref="GetPageAsync"/> for the current page only (skipped when the count is zero), so a grid over millions of
/// rows loads just the rows it shows.
/// </summary>
/// <remarks>
/// Keep implementations free of per-fetch state: open and close any connection or context inside each call.
/// </remarks>
public interface IGridDataSource<TDataItem> where TDataItem : class
{
    /// <summary>Counts the rows matching <see cref="GridDataSourceQuery{TDataItem}.Filter"/>.</summary>
    Task<int> CountAsync(GridDataSourceQuery<TDataItem> query, CancellationToken cancellationToken = default);

    /// <summary>Loads one page: includes, filter, order, then skip and take.</summary>
    /// <remarks>
    /// The order must be deterministic: break ties with a unique key, otherwise rows with equal sort values can
    /// repeat on two pages or appear on none.
    /// </remarks>
    Task<IReadOnlyList<TDataItem>> GetPageAsync(GridDataSourceQuery<TDataItem> query, CancellationToken cancellationToken = default);
}

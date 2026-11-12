namespace Maviray.Blazor.Components.Pro.Core.Access;

/// <summary>
/// Told about every grid fetch after it completes, for example to audit who viewed which rows. Observers run inside
/// the fetch, in registration order, and their exceptions fail the fetch; an observer that must not affect the grid
/// catches its own exceptions, and slow work (writing an audit store) belongs on a background queue.
/// </summary>
public interface IGridFetchObserver
{
    /// <summary>A page was loaded; called for empty pages too.</summary>
    ValueTask OnRowsLoadedAsync<TDataItem, TKey>(GridRowsLoaded<TKey> loaded, CancellationToken cancellationToken)
        where TDataItem : class
        where TKey : IEquatable<TKey> =>
        ValueTask.CompletedTask;

    /// <summary>A fetch was denied by an access policy or by <c>GridDataFactoryOptions.RequireExplicitAllow</c>.</summary>
    ValueTask OnAccessDeniedAsync<TDataItem>(GridAccessDenied denied, CancellationToken cancellationToken)
        where TDataItem : class =>
        ValueTask.CompletedTask;
}

/// <summary>The rows a fetch loaded.</summary>
/// <param name="RowKeys">The keys of the page's rows, in display order.</param>
public sealed record GridRowsLoaded<TKey>(GridAccessRequest Request, IReadOnlyList<TKey> RowKeys, int PageNumber, int PageSize, int TotalRows)
    where TKey : IEquatable<TKey>;

/// <summary>A denied fetch, with the denying policy's message if it gave one.</summary>
public sealed record GridAccessDenied(GridAccessRequest Request, string? Message);

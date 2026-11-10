using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Interfaces;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Services;

public enum DemoAccessMode
{
    Full,
    Narrowed,
    None
}

/// <summary>The access level chosen on the Access page; one per circuit.</summary>
public sealed class DemoAccessState
{
    public DemoAccessMode Mode { get; set; } = DemoAccessMode.Full;
}

/// <summary>
/// Demo policy for the grid tagged <see cref="AccessTag"/>: full access, scope "Alpha" only, or no access, as chosen
/// on the page. Every other grid is not its concern. A real policy would read the user's grants instead.
/// </summary>
public sealed class DemoAccessPolicy(DemoAccessState state) : IGridAccessPolicy
{
    public const string AccessTag = "access-demo";

    private static readonly Expression<Func<IScopedItem, bool>> ScopeAlphaOnly = x => x.ScopeId == 1;

    public ValueTask<GridAccessDecision<TDataItem>> EvaluateAsync<TDataItem>(GridAccessRequest request, CancellationToken cancellationToken)
        where TDataItem : class
    {
        if (request.AccessTag != AccessTag)
        {
            return ValueTask.FromResult(GridAccessDecision<TDataItem>.NotApplicable());
        }

        var decision = state.Mode switch
        {
            DemoAccessMode.None => GridAccessDecision<TDataItem>.Deny("Your demo role has no access to these items."),
            DemoAccessMode.Narrowed when typeof(IScopedItem).IsAssignableFrom(typeof(TDataItem)) =>
                GridAccessDecision<TDataItem>.Allow((Expression<Func<TDataItem, bool>>)GridFilterExpression.ForInterface(typeof(TDataItem), ScopeAlphaOnly)),
            _ => GridAccessDecision<TDataItem>.Allow()
        };

        return ValueTask.FromResult(decision);
    }
}

/// <summary>The last few fetches of the Access page's grid, newest first; one per circuit.</summary>
public sealed class DemoFetchLog
{
    private readonly LinkedList<string> _entries = new();

    public IReadOnlyCollection<string> Entries => _entries;

    public void Add(string entry)
    {
        _entries.AddFirst($"{DateTime.Now:HH:mm:ss} {entry}");

        while (_entries.Count > 5)
        {
            _entries.RemoveLast();
        }
    }
}

/// <summary>Demo audit: records which rows the Access page's grid loaded, or that it was denied.</summary>
public sealed class DemoFetchObserver(DemoFetchLog log) : IGridFetchObserver
{
    public ValueTask OnRowsLoadedAsync<TDataItem, TKey>(GridRowsLoaded<TKey> loaded, CancellationToken cancellationToken)
        where TDataItem : class
        where TKey : IEquatable<TKey>
    {
        if (loaded.Request.AccessTag == DemoAccessPolicy.AccessTag)
        {
            log.Add($"Page {loaded.PageNumber}: keys {string.Join(", ", loaded.RowKeys)} of {loaded.TotalRows}");
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnAccessDeniedAsync<TDataItem>(GridAccessDenied denied, CancellationToken cancellationToken)
        where TDataItem : class
    {
        if (denied.Request.AccessTag == DemoAccessPolicy.AccessTag)
        {
            log.Add($"Access denied: {denied.Message}");
        }

        return ValueTask.CompletedTask;
    }
}

using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Services;

/// <summary>What the documentation page's access demo currently allows; one per circuit.</summary>
public sealed class DocsAccessState
{
    public bool OnlySmallAmounts { get; set; } = true;

    public bool HideCreatedColumn { get; set; } = true;
}

/// <summary>
/// The documentation page's access policy. It governs only grids over <see cref="DbSampleItemOne"/> tagged
/// <see cref="AccessTag"/>: it can narrow them to amounts below 500 and hide their "Date" column.
/// </summary>
public sealed class DocsAccessPolicy(DocsAccessState state) : GridAccessPolicy<DbSampleItemOne>
{
    public const string AccessTag = "docs-access";

    protected override ValueTask<GridAccessDecision<DbSampleItemOne>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken)
    {
        if (request.AccessTag != AccessTag)
        {
            return ValueTask.FromResult(GridAccessDecision<DbSampleItemOne>.NotApplicable());
        }

        var decision = state.OnlySmallAmounts
            ? GridAccessDecision<DbSampleItemOne>.Allow(item => item.Integer < 500)
            : GridAccessDecision<DbSampleItemOne>.Allow();

        return ValueTask.FromResult(state.HideCreatedColumn ? decision.WithHiddenColumns("Date") : decision);
    }
}

/// <summary>The documentation page's audit trail: the last few fetches of its access demo, newest first.</summary>
public sealed class DocsAuditLog
{
    private readonly LinkedList<string> _entries = new();

    public IReadOnlyCollection<string> Entries => _entries;

    public void Add(string entry)
    {
        _entries.AddFirst($"{DateTime.Now:HH:mm:ss}  {entry}");

        while (_entries.Count > 4)
        {
            _entries.RemoveLast();
        }
    }
}

/// <summary>Records which rows the documentation page's access demo loaded.</summary>
public sealed class DocsAuditObserver(DocsAuditLog log) : IGridFetchObserver
{
    public ValueTask OnRowsLoadedAsync<TDataItem, TKey>(GridRowsLoaded<TKey> loaded, CancellationToken cancellationToken)
        where TDataItem : class
        where TKey : IEquatable<TKey>
    {
        if (loaded.Request.AccessTag == DocsAccessPolicy.AccessTag)
        {
            log.Add($"{typeof(TDataItem).Name} page {loaded.PageNumber}: keys {string.Join(", ", loaded.RowKeys)} of {loaded.TotalRows}");
        }

        return ValueTask.CompletedTask;
    }
}

using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

/// <summary>In-memory data source that records every call it receives.</summary>
public sealed class FakeGridDataSource(IReadOnlyList<TestOrder> items) : IGridDataSource<TestOrder>
{
    public int CountCalls { get; private set; }
    public int GetCalls { get; private set; }
    public GridDataSourceQuery<TestOrder>? LastCountQuery { get; private set; }
    public GridDataSourceQuery<TestOrder>? LastGetQuery { get; private set; }
    public CancellationToken LastCountToken { get; private set; }
    public CancellationToken LastGetToken { get; private set; }

    public Task<int> CountAsync(GridDataSourceQuery<TestOrder> query, CancellationToken cancellationToken = default)
    {
        CountCalls++;
        LastCountQuery = query;
        LastCountToken = cancellationToken;

        return Task.FromResult(Filtered(query).Count());
    }

    public Task<IReadOnlyList<TestOrder>> GetPageAsync(GridDataSourceQuery<TestOrder> query, CancellationToken cancellationToken = default)
    {
        GetCalls++;
        LastGetQuery = query;
        LastGetToken = cancellationToken;

        var filtered = Filtered(query);
        var ordered = query.Sort is { } sort
            ? filtered.OrderByDynamic(sort.Path, sort.Order, sort.NullDefault).ThenByDynamic(nameof(TestOrder.Id), sort.Order)
            : filtered.OrderBy(x => x.Id);

        var sliced = ordered.Skip(query.Skip).Take(query.Take);
        IReadOnlyList<TestOrder> page = query.SelectPaths is { } paths ? sliced.SelectPaths(paths).ToList() : sliced.ToList();
        return Task.FromResult(page);
    }

    private IQueryable<TestOrder> Filtered(GridDataSourceQuery<TestOrder> query)
    {
        var source = items.AsQueryable();
        return query.Filter is null ? source : source.Where(query.Filter);
    }
}

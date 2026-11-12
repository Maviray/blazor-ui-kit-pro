using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

/// <summary>In-memory keyset data source over orders, ordered by the sort then Id; records the page queries.</summary>
public sealed class FakeKeysetDataSource(IReadOnlyList<TestOrder> items) : IGridKeysetDataSource<TestOrder>
{
    private readonly FakeGridDataSource _inner = new(items);

    public int CountCalls => _inner.CountCalls;
    public List<GridDataSourceQuery<TestOrder>> PageQueries { get; } = [];

    public Task<int> CountAsync(GridDataSourceQuery<TestOrder> query, CancellationToken cancellationToken = default) =>
        _inner.CountAsync(query, cancellationToken);

    public Task<IReadOnlyList<TestOrder>> GetPageAsync(GridDataSourceQuery<TestOrder> query, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The factory should load pages through GetKeysetPageAsync.");

    public Task<GridKeysetPage<TestOrder>> GetKeysetPageAsync(GridDataSourceQuery<TestOrder> query, CancellationToken cancellationToken = default)
    {
        PageQueries.Add(query);

        GridOrderKey[] keys = query.Sort is { } sort
            ? [new GridOrderKey(sort.Path, sort.Order, sort.NullDefault), new GridOrderKey(nameof(TestOrder.Id), sort.Order)]
            : [new GridOrderKey(nameof(TestOrder.Id), SortOrder.Ascending)];

        var source = query.Filter is null ? items.AsQueryable() : items.AsQueryable().Where(query.Filter);

        return new GridKeyset<TestOrder>(keys).LoadPageAsync(source, query.Cursor, query.Skip, query.Take, (q, _) => Task.FromResult(q.ToList()), cancellationToken);
    }
}

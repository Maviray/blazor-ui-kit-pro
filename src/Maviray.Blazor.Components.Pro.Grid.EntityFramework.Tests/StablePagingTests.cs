using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests;

public sealed class StablePagingTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _db = new();

    public StablePagingTests()
    {
        _db.Seed(context =>
        {
            context.Products.AddRange(Enumerable.Range(1, 25).Select(id => new TestProduct { Id = id, Name = $"P{id}", Category = $"C{id % 3}" }));
            context.Lines.AddRange(Enumerable.Range(1, 6).Select(n => new TestLine { OrderNumber = n % 2, LineNumber = n, Sku = "same" }));
        });
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private Task<IReadOnlyList<TestProduct>> Page(GridSort? sort, int skip, int take = 10) =>
        _db.CreateSource<TestProduct>().GetPageAsync(new GridDataSourceQuery<TestProduct> { Sort = sort, Skip = skip, Take = take }, Ct);

    [Fact]
    public async Task SortOnADuplicateColumn_ReturnsEveryRowExactlyOnce_InTheSameOrderEachTime()
    {
        var sort = new GridSort("Category", SortOrder.Ascending);

        async Task<List<int>> AllPages() => [.. (await Page(sort, 0)).Concat(await Page(sort, 10)).Concat(await Page(sort, 20)).Select(p => p.Id)];

        var first = await AllPages();
        var second = await AllPages();

        Assert.Equal(Enumerable.Range(1, 25), first.Order());
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Ties_FollowThePrimaryKey_InTheSortDirection()
    {
        var rows = await Page(new GridSort("Category", SortOrder.Descending), 0, 25);

        foreach (var group in rows.GroupBy(r => r.Category))
        {
            Assert.Equal(group.Select(r => r.Id).OrderDescending(), group.Select(r => r.Id));
        }
    }

    [Fact]
    public async Task SortByTheWholeKey_AddsNoTieBreaker()
    {
        await Page(new GridSort("Id", SortOrder.Ascending), 0);

        var orderBy = Assert.Single(_db.Commands).OrderByClause;
        Assert.Equal(1, orderBy.Split("\"Id\"").Length - 1);
    }

    [Fact]
    public async Task CompositeKey_AppendsEachKeyProperty_InKeyOrder()
    {
        await _db.CreateSource<TestLine>().GetPageAsync(new GridDataSourceQuery<TestLine> { Sort = new GridSort("Sku", SortOrder.Ascending), Take = 10 }, Ct);

        var orderBy = Assert.Single(_db.Commands).OrderByClause;
        Assert.True(orderBy.IndexOf("\"OrderNumber\"", StringComparison.Ordinal) is > 0 and var number && number < orderBy.IndexOf("\"LineNumber\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NoSort_OrdersByThePrimaryKey()
    {
        var rows = await Page(sort: null, skip: 0, take: 25);

        Assert.Equal(Enumerable.Range(1, 25), rows.Select(r => r.Id));
        Assert.Contains("\"Id\"", Assert.Single(_db.Commands).OrderByClause);
    }

    [Fact]
    public async Task KeylessType_GetsNoTieBreaker_AndDoesNotThrow()
    {
        var rows = await _db.CreateSource<TestProductSummary>().GetPageAsync(
            new GridDataSourceQuery<TestProductSummary> { Sort = new GridSort("Category", SortOrder.Ascending), Take = 10 }, Ct);

        Assert.Equal(10, rows.Count);
        Assert.DoesNotContain("\"Name\"", Assert.Single(_db.Commands).OrderByClause);
    }

    [Fact]
    public async Task CancelledToken_StopsTheQuery()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _db.CreateSource<TestProduct>().GetPageAsync(new GridDataSourceQuery<TestProduct> { Take = 10 }, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task IncludeShaper_AppliesToTheCountAsWellAsThePage()
    {
        var query = new GridDataSourceQuery<TestProduct> { Include = q => q.Where(p => p.Category == "C1"), Take = 25 };
        var source = _db.CreateSource<TestProduct>();

        var count = await source.CountAsync(query, Ct);
        var page = await source.GetPageAsync(query, Ct);

        Assert.Equal(9, page.Count);
        Assert.Equal(page.Count, count);
    }
}

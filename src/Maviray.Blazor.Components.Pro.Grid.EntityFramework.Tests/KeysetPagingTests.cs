using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests;

/// <summary>The EF data source seeking from cursors against SQLite.</summary>
public sealed class KeysetPagingTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _db = new();

    public KeysetPagingTests()
    {
        _db.Seed(context =>
        {
            // Three categories, so most sort values repeat and the primary key decides the order within them.
            context.Products.AddRange(Enumerable.Range(1, 25).Select(id => new TestProduct { Id = id, Name = $"P{id}", Category = $"C{id % 3}" }));
            context.Lines.AddRange(Enumerable.Range(1, 6).Select(n => new TestLine { OrderNumber = n % 2, LineNumber = n, Sku = "same" }));
            context.Documents.AddRange(Enumerable.Range(1, 4).Select(id => new TestDocument { Id = id, Title = $"D{id}", Note = id % 2 == 0 ? null : "n" }));
            context.Documents.AddRange(Enumerable.Range(5, 7).Select(id => new TestDocument
            {
                Id = id, Title = $"D{id}", Note = "n", Kind = (TestDocumentKind)(id % 3), Archived = id % 2 == 0, KindAsText = (TestDocumentKind)(id % 3)
            }));
            context.Tags.AddRange(Enumerable.Range(1, 7).Select(n => new TestTag { Id = Guid.NewGuid(), Label = $"t{n}" }));
        });
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private static readonly GridSort ByCategory = new("Category", SortOrder.Ascending);

    private Task<GridKeysetPage<T>> Load<T>(GridSort? sort, MaviGridCursor? cursor = null, int skip = 0, int take = 10) where T : class =>
        _db.CreateSource<T>().GetKeysetPageAsync(new GridDataSourceQuery<T> { Sort = sort, Cursor = cursor, Skip = skip, Take = take }, Ct);

    private static MaviGridCursor Next(GridKeysetPage<TestProduct> page) => new(page.LastRowCursor!, MaviGridCursorDirection.Next);

    private static MaviGridCursor Previous(GridKeysetPage<TestProduct> page) => new(page.FirstRowCursor!, MaviGridCursorDirection.Previous);

    private async Task<List<int>> OffsetOrder(GridSort sort)
    {
        var all = await _db.CreateSource<TestProduct>().GetPageAsync(new GridDataSourceQuery<TestProduct> { Sort = sort, Take = 100 }, Ct);
        return [.. all.Select(p => p.Id)];
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public async Task WalkingForwardByCursor_GivesTheOffsetOrder(SortOrder order)
    {
        var sort = new GridSort("Category", order);
        var first = await Load<TestProduct>(sort);
        var second = await Load<TestProduct>(sort, Next(first), skip: 999);
        var third = await Load<TestProduct>(sort, Next(second), skip: 999);

        Assert.Equal(await OffsetOrder(sort), first.Items.Concat(second.Items).Concat(third.Items).Select(p => p.Id));
        Assert.Equal(5, third.Items.Count);
    }

    [Fact]
    public async Task WalkingBackwardByCursor_GivesTheSamePages()
    {
        var third = await Load<TestProduct>(ByCategory, skip: 20);
        var second = await Load<TestProduct>(ByCategory, Previous(third), skip: 999);
        var first = await Load<TestProduct>(ByCategory, Previous(second), skip: 999);

        Assert.Equal(await OffsetOrder(ByCategory), first.Items.Concat(second.Items).Concat(third.Items).Select(p => p.Id));
    }

    [Fact]
    public async Task Seeking_UsesAWhereInsteadOfAnOffset_WithTheValuesAsParameters()
    {
        var first = await Load<TestProduct>(ByCategory);
        _db.ClearCommands();

        await Load<TestProduct>(ByCategory, Next(first));

        var sql = Assert.Single(_db.Commands);
        Assert.DoesNotContain("OFFSET", sql.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", sql.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain($"'{first.Items[^1].Category}'", sql.Sql);
        Assert.Contains(first.Items[^1].Category, sql.ParameterValues);
    }

    [Fact]
    public async Task NoSort_SeeksOnThePrimaryKey()
    {
        var first = await Load<TestProduct>(sort: null);

        var second = await Load<TestProduct>(sort: null, Next(first), skip: 999);

        Assert.Equal(Enumerable.Range(11, 10), second.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task CompositeKey_SeeksOnEveryKeyProperty()
    {
        var sort = new GridSort("Sku", SortOrder.Ascending);
        var seen = new List<(int, int)>();
        MaviGridCursor? cursor = null;

        for (var i = 0; i < 3; i++)
        {
            var page = await Load<TestLine>(sort, cursor, skip: 999 * i, take: 2);
            seen.AddRange(page.Items.Select(l => (l.OrderNumber, l.LineNumber)));
            cursor = new MaviGridCursor(page.LastRowCursor!, MaviGridCursorDirection.Next);
        }

        Assert.Equal([(0, 2), (0, 4), (0, 6), (1, 1), (1, 3), (1, 5)], seen);
    }

    [Fact]
    public async Task NullableSortColumn_IsPagedByOffset_WithoutCursors()
    {
        var page = await Load<TestDocument>(new GridSort("Note", SortOrder.Ascending), take: 2);

        Assert.Equal(2, page.Items.Count);
        Assert.Null(page.FirstRowCursor);
        Assert.Null(page.LastRowCursor);
    }

    [Fact]
    public async Task NullableSortColumnWithANullDefault_IsSought()
    {
        var sort = new GridSort("Note", SortOrder.Ascending, NullDefault: "");
        var first = await Load<TestDocument>(sort, take: 2);

        var second = await Load<TestDocument>(sort, new MaviGridCursor(first.LastRowCursor!, MaviGridCursorDirection.Next), skip: 999, take: 2);

        // Null notes sort as "" (before "n"), by Id within each.
        Assert.Equal([2, 4], first.Items.Select(d => d.Id));
        Assert.Equal([1, 3], second.Items.Select(d => d.Id));
    }

    // Every page by cursor, forward or backward, against the same rows in the database's offset order.
    private async Task AssertWalkMatchesOffsetOrder<T>(GridSort? sort, Func<T, object> id, bool backward) where T : class
    {
        const int take = 2;
        var source = _db.CreateSource<T>();
        var all = await source.GetPageAsync(new GridDataSourceQuery<T> { Sort = sort, Take = 100 }, Ct);
        var page = await Load<T>(sort, skip: backward ? (all.Count - 1) / take * take : 0, take: take);
        var pages = new List<IReadOnlyList<T>> { page.Items };

        while (pages.Sum(p => p.Count) < all.Count)
        {
            Assert.NotNull(backward ? page.FirstRowCursor : page.LastRowCursor);
            var cursor = backward
                ? new MaviGridCursor(page.FirstRowCursor!, MaviGridCursorDirection.Previous)
                : new MaviGridCursor(page.LastRowCursor!, MaviGridCursorDirection.Next);
            page = await Load<T>(sort, cursor, skip: 999, take: take);
            Assert.NotEmpty(page.Items);
            pages.Add(page.Items);
        }

        if (backward)
        {
            pages.Reverse();
        }

        Assert.Equal(all.Select(id), pages.SelectMany(p => p).Select(id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task GuidKey_IsSought_InTheDatabasesOrder(bool backward) =>
        AssertWalkMatchesOffsetOrder<TestTag>(sort: null, t => t.Id, backward);

    [Theory]
    [InlineData(SortOrder.Ascending, false)]
    [InlineData(SortOrder.Descending, true)]
    public Task GuidKeyBreakingTiesOfASort_IsSought(SortOrder order, bool backward) =>
        AssertWalkMatchesOffsetOrder<TestTag>(new GridSort("Label", order), t => t.Id, backward);

    [Theory]
    [InlineData(SortOrder.Ascending, false)]
    [InlineData(SortOrder.Ascending, true)]
    [InlineData(SortOrder.Descending, false)]
    public Task EnumSort_IsSought(SortOrder order, bool backward) =>
        AssertWalkMatchesOffsetOrder<TestDocument>(new GridSort("Kind", order), d => d.Id, backward);

    [Theory]
    [InlineData(SortOrder.Ascending, false)]
    [InlineData(SortOrder.Ascending, true)]
    [InlineData(SortOrder.Descending, false)]
    public Task BoolSort_IsSought(SortOrder order, bool backward) =>
        AssertWalkMatchesOffsetOrder<TestDocument>(new GridSort("Archived", order), d => d.Id, backward);

    [Fact]
    public async Task ProjectionWithoutTheKey_StillSeeksOnIt()
    {
        // Only Category is selected and it repeats: the cursors need the primary key, which the data source adds.
        var source = _db.CreateSource<TestProduct>();
        GridDataSourceQuery<TestProduct> Query(MaviGridCursor? cursor) => new() { Sort = ByCategory, Cursor = cursor, Take = 10, Skip = cursor is null ? 0 : 999, SelectPaths = ["Category"] };

        var first = await source.GetKeysetPageAsync(Query(null), Ct);
        var second = await source.GetKeysetPageAsync(Query(Next(first)), Ct);
        var third = await source.GetKeysetPageAsync(Query(Next(second)), Ct);

        Assert.Equal(await OffsetOrder(ByCategory), first.Items.Concat(second.Items).Concat(third.Items).Select(p => p.Id));
    }

    [Fact]
    public async Task SortColumnWithAValueConverter_IsPagedByOffset_WithoutCursors()
    {
        var page = await Load<TestDocument>(new GridSort("KindAsText", SortOrder.Ascending), take: 2);

        Assert.Equal(2, page.Items.Count);
        Assert.Null(page.LastRowCursor);
    }

    [Fact]
    public async Task CursorOfAnotherSort_IsIgnored()
    {
        var byCategory = await Load<TestProduct>(ByCategory);

        var page = await Load<TestProduct>(new GridSort("Name", SortOrder.Ascending), Next(byCategory), skip: 10);

        Assert.Equal((await _db.CreateSource<TestProduct>().GetPageAsync(
            new GridDataSourceQuery<TestProduct> { Sort = new GridSort("Name", SortOrder.Ascending), Skip = 10, Take = 10 }, Ct)).Select(p => p.Id),
            page.Items.Select(p => p.Id));
    }
}

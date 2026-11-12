using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

/// <summary>The factory over a keyset data source: cursors out to the grid, cursors back in to the data source.</summary>
public class GridDataFactoryKeysetTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static MaviGridQuery Page(int number, MaviGridCursor? cursor = null, int? knownTotal = null) =>
        new() { PageNumber = number, PageSize = 10, Cursor = cursor, KnownTotalRows = knownTotal };

    [Fact]
    public async Task Pages_CarryCursors_ExceptPastEitherEnd()
    {
        var factory = TestFactory.Create(new FakeKeysetDataSource(TestFactory.Orders(25)));

        var first = await factory.BuildPageAsync(Page(1), cancellationToken: Ct);
        var last = await factory.BuildPageAsync(Page(3), cancellationToken: Ct);

        Assert.NotNull(first.NextPageCursor);
        Assert.Null(first.PreviousPageCursor);
        Assert.Null(last.NextPageCursor);
        Assert.NotNull(last.PreviousPageCursor);
    }

    [Fact]
    public async Task NextCursor_SeeksToTheNextPage()
    {
        var source = new FakeKeysetDataSource(TestFactory.Orders(25));
        var factory = TestFactory.Create(source);
        var first = await factory.BuildPageAsync(Page(1), cancellationToken: Ct);
        var cursor = new MaviGridCursor(first.NextPageCursor!, MaviGridCursorDirection.Next);

        var second = await factory.BuildPageAsync(Page(2, cursor, knownTotal: 25), cancellationToken: Ct);

        Assert.Equal(cursor, source.PageQueries[^1].Cursor);
        Assert.Equal(Enumerable.Range(11, 10), second.Rows.Select(r => r.Key));
        Assert.Equal(2, second.PageNumber);
    }

    [Fact]
    public async Task PreviousCursor_SeeksToThePreviousPage()
    {
        var factory = TestFactory.Create(new FakeKeysetDataSource(TestFactory.Orders(25)));
        var third = await factory.BuildPageAsync(Page(3), cancellationToken: Ct);

        var second = await factory.BuildPageAsync(Page(2, new MaviGridCursor(third.PreviousPageCursor!, MaviGridCursorDirection.Previous)), cancellationToken: Ct);

        Assert.Equal(Enumerable.Range(11, 10), second.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task CursorForAPageThatWasClamped_IsNotUsed()
    {
        var source = new FakeKeysetDataSource(TestFactory.Orders(25));
        var factory = TestFactory.Create(source);
        var first = await factory.BuildPageAsync(Page(1), cancellationToken: Ct);

        // Page 9 doesn't exist; the factory loads page 3, which the cursor doesn't lead to.
        var page = await factory.BuildPageAsync(Page(9, new MaviGridCursor(first.NextPageCursor!, MaviGridCursorDirection.Next)), cancellationToken: Ct);

        Assert.Null(source.PageQueries[^1].Cursor);
        Assert.Equal(Enumerable.Range(21, 5), page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task StaleKnownTotal_ReloadsTheNewLastPageWithoutTheCursor()
    {
        // The grid saw 30 rows and seeks from page 2 to page 3; 15 were deleted since, so page 3 is gone. (Page 2
        // of 15 rows is the last page and has no next cursor; its previous cursor stands in for the stale one.)
        var source = new FakeKeysetDataSource(TestFactory.Orders(15));
        var factory = TestFactory.Create(source);
        var second = await factory.BuildPageAsync(Page(2), cancellationToken: Ct);

        var page = await factory.BuildPageAsync(Page(3, new MaviGridCursor(second.PreviousPageCursor!, MaviGridCursorDirection.Next), knownTotal: 30), cancellationToken: Ct);

        Assert.Equal(2, page.PageNumber);
        Assert.Equal(Enumerable.Range(11, 5), page.Rows.Select(r => r.Key));
        Assert.Null(source.PageQueries[^1].Cursor);
    }

    [Fact]
    public async Task DataSourceWithoutKeyset_GivesNoCursors()
    {
        var page = await TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(25))).BuildPageAsync(Page(2), cancellationToken: Ct);

        Assert.Null(page.NextPageCursor);
        Assert.Null(page.PreviousPageCursor);
    }
}

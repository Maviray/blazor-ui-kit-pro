using Bunit;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests;

/// <summary>The hints the grid sends so fetch code can skip the count (and seek instead of skipping rows).</summary>
public class MaviGridFastPagingTests : MaviGridTestContext
{
    private static MaviGridParameters Filterable() => new() { EnableFilters = true, FilterDebounceMilliseconds = 0 };

    // Count reuse

    [Fact]
    public void FirstFetch_HasNoKnownTotal()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 30, 1));

        RenderGrid(fetch.Invoke);

        Assert.Null(Assert.Single(fetch.Queries).KnownTotalRows);
    }

    [Fact]
    public void PageSortAndPageSizeChanges_SendTheTotalOfTheCurrentPage()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 30, 1));
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("[data-page-next] button").Click();
        grid.Find("[data-sort-key='Name']").Click();
        grid.Find("select[data-page-size]").Change("25");

        Assert.Equal([null, 30, 30, 30], fetch.Queries.Select(q => q.KnownTotalRows));
    }

    [Fact]
    public void FilterChange_SendsNoKnownTotal()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 30, 1));
        var grid = RenderGrid(fetch.Invoke, Filterable());

        grid.Find("input[data-filter-key='Name']").Input("Ab");
        grid.WaitForAssertion(() => Assert.Equal(2, fetch.Queries.Count));
        grid.Find("[data-page-next] button").Click();

        Assert.Null(fetch.Queries[1].KnownTotalRows);
        Assert.Equal(30, fetch.Queries[2].KnownTotalRows);
    }

    [Fact]
    public async Task Refresh_SendsNoKnownTotal()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 30, 1));
        var grid = RenderGrid(fetch.Invoke);

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Null(fetch.Queries[^1].KnownTotalRows);
    }

    [Fact]
    public void ReloadFromTheNoDataState_SendsNoKnownTotal_EvenWithTheSameFilters()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 0));
        var grid = RenderGrid(fetch.Invoke, Filterable());

        grid.Find("[data-grid-state='no-data'] button").Click();

        Assert.Equal(2, fetch.Queries.Count);
        Assert.Null(fetch.Queries[^1].KnownTotalRows);
    }

    [Fact]
    public async Task FetchAfterAFailure_SendsNoKnownTotal()
    {
        var fetch = new RecordingFetch((q, call) => call == 2 ? throw new InvalidOperationException() : GridPages.PageOf(q, 30, 1));
        var grid = RenderGrid(fetch.Invoke);

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());
        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Null(fetch.Queries[2].KnownTotalRows);
    }

    // Keyset cursors

    private static RecordingFetch CursorFetch() => new((q, _) => GridPages.WithCursors(q, 50));

    [Fact]
    public void NextPage_SendsTheCurrentPagesNextCursor()
    {
        var fetch = CursorFetch();
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("[data-page-next] button").Click();
        grid.Find("[data-page-next] button").Click();

        Assert.Null(fetch.Queries[0].Cursor);
        Assert.Equal(new MaviGridCursor("after-10", MaviGridCursorDirection.Next), fetch.Queries[1].Cursor);
        Assert.Equal(new MaviGridCursor("after-20", MaviGridCursorDirection.Next), fetch.Queries[2].Cursor);
        Assert.Equal(3, fetch.Queries[2].PageNumber);
    }

    [Fact]
    public void PreviousPage_SendsTheCurrentPagesPreviousCursor()
    {
        var fetch = CursorFetch();
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("input[data-page-goto]").Change("4");
        grid.Find("[data-page-prev] button").Click();

        Assert.Equal(new MaviGridCursor("before-31", MaviGridCursorDirection.Previous), fetch.Queries[^1].Cursor);
        Assert.Equal(3, fetch.Queries[^1].PageNumber);
    }

    [Fact]
    public void BackToTheFirstPage_SendsNoCursor()
    {
        // Skipping no rows is as fast as seeking, and it also shows rows added before the old first row.
        var fetch = CursorFetch();
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("[data-page-next] button").Click();
        grid.Find("[data-page-prev] button").Click();

        Assert.Equal(1, fetch.Queries[^1].PageNumber);
        Assert.Null(fetch.Queries[^1].Cursor);
    }

    [Fact]
    public void JumpingMoreThanOnePage_SendsNoCursor()
    {
        var fetch = CursorFetch();
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("input[data-page-goto]").Change("3");

        Assert.Null(fetch.Queries[^1].Cursor);
    }

    [Fact]
    public void GoingToTheAdjacentPageByNumber_SendsTheCursor()
    {
        var fetch = CursorFetch();
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("input[data-page-goto]").Change("2");

        Assert.Equal(new MaviGridCursor("after-10", MaviGridCursorDirection.Next), fetch.Queries[^1].Cursor);
    }

    [Fact]
    public async Task Refresh_SendsNoCursor()
    {
        var fetch = CursorFetch();
        var grid = RenderGrid(fetch.Invoke);
        grid.Find("[data-page-next] button").Click();

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Null(fetch.Queries[^1].Cursor);
    }

    [Fact]
    public void PageWithoutCursors_SendsNoCursor()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 30, 1));
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("[data-page-next] button").Click();

        Assert.Null(fetch.Queries[^1].Cursor);
    }
}

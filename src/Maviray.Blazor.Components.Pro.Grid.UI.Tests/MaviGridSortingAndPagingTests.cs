using Bunit;
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests;

public class MaviGridSortingAndPagingTests : MaviGridTestContext
{
    [Fact]
    public void FirstFetch_UsesTheInitialSortAndPageSizeFromParameters()
    {
        var fetch = new RecordingFetch(1);

        RenderGrid(fetch.Invoke, new MaviGridParameters { SortColumnKey = "Name", SortOrder = SortOrder.Descending, PageSize = 25 });

        var query = Assert.Single(fetch.Queries);
        Assert.Equal(("Name", SortOrder.Descending, 25, 1), (query.SortColumnKey, query.SortOrder, query.PageSize, query.PageNumber));
    }

    [Fact]
    public void ClickingASortableHeader_SortsAscending_ThenToggles_AndReturnsToTheFirstPage()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 30, 1, 2));
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("[data-page-next] button").Click();
        grid.Find("[data-sort-key='Name']").Click();
        grid.Find("[data-sort-key='Name']").Click();

        Assert.Equal(2, fetch.Queries[1].PageNumber);
        Assert.Equal(("Name", SortOrder.Ascending, 1), (fetch.Queries[2].SortColumnKey, fetch.Queries[2].SortOrder, fetch.Queries[2].PageNumber));
        Assert.Equal(SortOrder.Descending, fetch.Queries[3].SortOrder);
    }

    [Fact]
    public async Task ParentRerender_KeepsTheUsersSort()
    {
        var fetch = new RecordingFetch(1);
        var parameters = new MaviGridParameters { SortColumnKey = "Id", SortOrder = SortOrder.Ascending };
        var grid = RenderGrid(fetch.Invoke, parameters);

        grid.Find("[data-sort-key='Id']").Click();
        grid.Render(b => b.Add(x => x.Parameters, parameters));
        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Equal(SortOrder.Descending, fetch.Queries[^1].SortOrder);
    }

    [Fact]
    public void SortedColumnHeader_ShowsTheDirectionTheServerApplied()
    {
        var grid = RenderGrid(new RecordingFetch(1).Invoke, new MaviGridParameters { SortColumnKey = "Name", SortOrder = SortOrder.Descending });

        Assert.Equal("descending", grid.Find("[data-sort-key='Name']").GetAttribute("data-sort-direction"));
        Assert.Null(grid.Find("[data-sort-key='Id']").GetAttribute("data-sort-direction"));
    }

    [Fact]
    public void ThePageTheServerReturns_IsTheBaseForTheNextPage()
    {
        // The server clamps any requested page to page 2.
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q with { PageNumber = 2 }, 30, 11, 12));
        var grid = RenderGrid(fetch.Invoke, new MaviGridParameters { PageSize = 10 });

        grid.Find("[data-page-next] button").Click();

        Assert.Equal(3, fetch.Queries[1].PageNumber);
    }

    [Fact]
    public void ChangingThePageSize_ReturnsToTheFirstPage()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 100, 1));
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("[data-page-next] button").Click();
        grid.Find("select[data-page-size]").Change("50");

        Assert.Equal((50, 1), (fetch.Queries[^1].PageSize, fetch.Queries[^1].PageNumber));
    }

    [Fact]
    public void GoToPage_IsClampedToTheAvailablePages()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 30, 1));
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("input[data-page-goto]").Change("99");

        Assert.Equal(3, fetch.Queries[^1].PageNumber);
    }

    [Fact]
    public void Pager_ShowsTheRowRange()
    {
        var grid = RenderGrid(new RecordingFetch((q, _) => GridPages.PageOf(q with { PageNumber = 2 }, 25, 11, 12)).Invoke);

        Assert.Contains("11–20 of 25", grid.Find("[data-pager]").TextContent);
    }

    [Fact]
    public void PageSizeTheServerApplies_IsShown_AndUsedForTheNextFetch()
    {
        // The server caps every page at 50 rows.
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q with { PageSize = Math.Min(q.PageSize, 50) }, 200, 1));
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("select[data-page-size]").Change("100");
        grid.Find("[data-page-next] button").Click();

        Assert.Equal("50", grid.Find("select[data-page-size] option[selected]").GetAttribute("value"));
        Assert.Equal(50, fetch.Queries[^1].PageSize);
    }

    [Fact]
    public void PageSizeNotAmongTheOptions_IsStillShown()
    {
        // The server caps every page at 40 rows, which isn't one of the offered sizes.
        var grid = RenderGrid(new RecordingFetch((q, _) => GridPages.PageOf(q with { PageSize = Math.Min(q.PageSize, 40) }, 200, 1)).Invoke);

        grid.Find("select[data-page-size]").Change("50");

        Assert.Equal("40", grid.Find("select[data-page-size] option[selected]").GetAttribute("value"));
        Assert.Equal(["10", "25", "40", "50", "100"], grid.FindAll("select[data-page-size] option").Select(o => o.GetAttribute("value")));
    }
}

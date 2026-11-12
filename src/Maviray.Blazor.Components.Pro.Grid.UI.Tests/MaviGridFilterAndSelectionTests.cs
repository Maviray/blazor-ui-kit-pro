using Bunit;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests;

public class MaviGridFilterAndSelectionTests : MaviGridTestContext
{
    private static MaviGridParameters Filterable() => new() { EnableFilters = true, FilterDebounceMilliseconds = 0 };

    [Fact]
    public void TypingInAStringFilter_FetchesWithAContainsFilter_FromTheFirstPage()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.PageOf(q, 30, 1));
        var grid = RenderGrid(fetch.Invoke, Filterable());
        grid.Find("[data-page-next] button").Click();

        grid.Find("input[data-filter-key='Name']").Input("Ab");

        grid.WaitForAssertion(() => Assert.Equal([new MaviGridColumnFilter("Name", MaviGridFilterCriteria.Contains, "Ab")], fetch.Queries[^1].Filters));
        Assert.Equal(1, fetch.Queries[^1].PageNumber);
    }

    [Fact]
    public void NumberFilter_WithTextThatIsNotANumber_SendsNoFilter()
    {
        var fetch = new RecordingFetch(1);
        var grid = RenderGrid(fetch.Invoke, Filterable());

        grid.Find("input[data-filter-key='Id']").Input("abc");

        grid.WaitForAssertion(() => Assert.Equal(2, fetch.Queries.Count));
        Assert.Empty(fetch.Queries[^1].Filters);
    }

    [Fact]
    public void FilterText_IsKeptAfterTheFetch()
    {
        var grid = RenderGrid(new RecordingFetch(1).Invoke, Filterable());

        grid.Find("input[data-filter-key='Name']").Input("Ab");

        grid.WaitForAssertion(() => Assert.Equal("Ab", grid.Find("input[data-filter-key='Name']").GetAttribute("value")));
    }

    [Fact]
    public void Selection_IsKeptAcrossPages()
    {
        var fetch = new RecordingFetch((q, _) => q.PageNumber == 1 ? GridPages.PageOf(q, 20, 1, 2) : GridPages.PageOf(q, 20, 11, 12));
        var grid = RenderGrid(fetch.Invoke, new MaviGridParameters { DisplaySelectColumn = true });

        grid.Find("[data-row-key='1'] input[type=checkbox]").Change(true);
        grid.Find("[data-page-next] button").Click();
        grid.Find("[data-page-prev] button").Click();

        Assert.Equal([1], grid.Instance.SelectedKeys);
        Assert.True(grid.Find("[data-row-key='1'] input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public void HiddenColumns_StayHiddenOnOtherPages()
    {
        var grid = RenderGrid(new RecordingFetch((q, _) => GridPages.PageOf(q, 30, q.PageNumber)).Invoke);

        grid.InvokeAsync(() => grid.Instance.SetColumnVisible("Name", false));
        grid.Find("[data-page-next] button").Click();

        Assert.Empty(grid.FindAll("[data-column-key='Name']"));
        Assert.Empty(grid.FindAll("[data-sort-key='Name']"));
        Assert.NotEmpty(grid.FindAll("[data-column-key='Id']"));
    }

    [Fact]
    public void ColumnsDialog_HidesTheUncheckedColumns_OnConfirm()
    {
        var grid = RenderGrid(new RecordingFetch(1).Invoke);

        grid.Find("[data-columns-toggle] button").Click();
        grid.Find("input[type=checkbox][aria-label='Name']").Change(false);
        grid.FindAll("button").Single(b => b.TextContent.Trim() == "Set").Click();

        Assert.Empty(grid.FindAll("[data-column-key='Name']"));
        Assert.Equal(["Name"], grid.Instance.HiddenColumnKeys);
        Assert.Empty(grid.FindAll("input[type=checkbox][aria-label='Name']"));
    }
}

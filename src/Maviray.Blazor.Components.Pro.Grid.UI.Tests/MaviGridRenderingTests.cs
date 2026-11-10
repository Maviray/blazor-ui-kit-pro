using Bunit;
using Maviray.Blazor.Components.Pro.Core.Models;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests;

public class MaviGridRenderingTests : MaviGridTestContext
{
    [Fact]
    public void RendersOneRowPerKey_WithACellPerColumn()
    {
        var grid = RenderGrid(new RecordingFetch(1, 2).Invoke);

        Assert.Equal(["1", "2"], RowKeys(grid));
        Assert.Equal("Name 2", grid.Find("[data-row-key='2'] [data-column-key='Name']").TextContent.Trim());
    }

    [Fact]
    public void SortableColumns_HaveASortButton_OthersDoNot()
    {
        var grid = RenderGrid(new RecordingFetch(1).Invoke);

        Assert.Equal("BUTTON", grid.Find("[data-sort-key='Name']").TagName);
        Assert.Empty(grid.FindAll("[data-sort-key='Note']"));
    }

    [Fact]
    public void FilterInputs_AreShownOnlyForFilterableColumns()
    {
        var grid = RenderGrid(new RecordingFetch(1).Invoke, new MaviGridParameters { EnableFilters = true });

        Assert.Equal(["Id", "Name"], grid.FindAll("input[data-filter-key]").Select(e => e.GetAttribute("data-filter-key")));
    }

    [Fact]
    public void OpeningTheMainMenu_DoesNotRerenderRows()
    {
        var grid = RenderGrid(new RecordingFetch(1, 2).Invoke, configure: b => b.Add(
            x => x.ConstructMainContextMenu,
            () => Task.FromResult<IEnumerable<Core.Interfaces.IMaviContextMenuItem>>([new MaviContextMenuItem { Title = "Export" }])));

        var rows = grid.FindComponents<MaviGridRowView<int>>();
        var before = rows.Select(r => r.RenderCount).ToArray();

        grid.Find("[data-main-menu-toggle] button").Click();

        Assert.Contains("Export", grid.Markup);
        Assert.Equal(before, rows.Select(r => r.RenderCount));
    }

    [Fact]
    public void Container_ReportsBusyWhileFetching()
    {
        var pending = new TaskCompletionSource<Core.Models.Grid.MaviGridCollection<int>>();
        var grid = RenderGrid((_, _) => pending.Task);

        Assert.Equal("true", grid.Find("[data-grid]").GetAttribute("aria-busy"));

        grid.InvokeAsync(() => pending.SetResult(GridPages.Page(new Core.Models.Grid.MaviGridQuery(), 1)));

        grid.WaitForAssertion(() => Assert.Equal("false", grid.Find("[data-grid]").GetAttribute("aria-busy")));
    }
}

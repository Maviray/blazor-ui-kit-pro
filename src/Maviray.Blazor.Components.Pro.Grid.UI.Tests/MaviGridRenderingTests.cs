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
    public void OpenMainMenu_IsPlacedAtTheClick_SoTheHeaderCellAndTheScrollingGridCannotClipIt()
    {
        // The header cell truncates (overflow: hidden) and the grid scrolls (overflow: auto): a dropdown positioned
        // inside them is cut off. Like the row menu, it is fixed to the viewport where the user clicked.
        var grid = RenderGrid(new RecordingFetch(1).Invoke, configure: b => b.Add(
            x => x.ConstructMainContextMenu,
            () => Task.FromResult<IEnumerable<Core.Interfaces.IMaviContextMenuItem>>([new MaviContextMenuItem { Title = "Export" }])));

        grid.Find("[data-main-menu-toggle] button").Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { ClientX = 500, ClientY = 100 });

        var menu = grid.Find("[data-main-menu]");
        Assert.Contains("fixed", menu.ClassList);
        Assert.DoesNotContain("absolute", menu.ClassList);
        Assert.Equal("top:108px;left:252px;", menu.GetAttribute("style"));
    }

    [Fact]
    public void SelectHeader_IsCenteredOverTheCheckboxes()
    {
        // Both justify-start and justify-center on one element: the one later in the stylesheet (start) wins.
        var grid = RenderGrid(new RecordingFetch(1).Invoke, new MaviGridParameters { DisplaySelectColumn = true });

        var header = grid.Find("[data-select-header]");
        Assert.Contains("justify-center", header.ClassList);
        Assert.DoesNotContain("justify-start", header.ClassList);
    }

    [Fact]
    public void PagerArrows_AreTheLeftAndRightChevrons()
    {
        var grid = RenderGrid(new RecordingFetch(1).Invoke);

        Assert.NotNull(grid.Find("[data-page-prev] .lni-chevron-left"));
        Assert.NotNull(grid.Find("[data-page-next] .lni-chevron-right"));
        Assert.Empty(grid.FindAll("[data-pager] .lni-flip-x"));
        // Free LineIcons 5.0 has no lni-chevron-right glyph: the button must still say what it does.
        Assert.Equal("Go to next page", grid.Find("[data-page-next] [title]").GetAttribute("title"));
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

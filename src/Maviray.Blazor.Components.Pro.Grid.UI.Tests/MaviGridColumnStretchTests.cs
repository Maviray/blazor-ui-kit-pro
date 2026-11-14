using Bunit;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests;

/// <summary>
/// How the columns share the width: one track per rendered column (data columns, select, actions). Stretching columns
/// take the free width by weight; with none visible, the actions column does.
/// </summary>
public class MaviGridColumnStretchTests : MaviGridTestContext
{
    private const string Fixed = "minmax(min-content, 200px)";
    private const string Fill = "minmax(min-content, 1fr)";

    private static string Tracks(IRenderedComponent<MaviGrid<int>> grid) =>
        grid.Find("[data-grid-body]").GetAttribute("style")!.Replace("grid-template-columns: ", "");

    private static MaviGridParameters With(IReadOnlyDictionary<string, int>? stretch = null, bool select = false, bool fill = true) =>
        new() { FilterDebounceMilliseconds = 0, StretchColumns = stretch, DisplaySelectColumn = select, FillWidth = fill };

    private static RecordingFetch FetchOf(IReadOnlyList<MaviGridColumn> columns) => new((q, _) => GridPages.PageWith(columns, q, 1, 1));

    [Fact]
    public void NoMarks_DataColumnsKeepTheirWidth_AndTheActionsColumnFills()
    {
        var grid = RenderGrid(FetchOf(GridPages.Columns).Invoke);

        Assert.Equal($"{Fixed} {Fixed} {Fixed} {Fill}", Tracks(grid));
    }

    [Fact]
    public void Container_FillsItsParent_ByDefault()
    {
        var grid = RenderGrid(FetchOf(GridPages.Columns).Invoke);

        var container = grid.Find("[data-grid]").ClassList;
        Assert.Contains("block", container);
        Assert.Contains("w-full", container);
        Assert.DoesNotContain("inline-block", container);
        Assert.DoesNotContain("max-w-full", container);
    }

    [Fact]
    public void FillWidthOff_ShrinksTheContainerToItsColumns()
    {
        var grid = RenderGrid(FetchOf(GridPages.Columns).Invoke, With(fill: false));

        var container = grid.Find("[data-grid]").ClassList;
        Assert.Contains("inline-block", container);
        Assert.Contains("max-w-full", container);
        Assert.DoesNotContain("block", container);
        Assert.DoesNotContain("w-full", container);
    }

    [Fact]
    public void OneStretchingColumn_TakesTheFreeWidth_AndTheActionsColumnShrinks()
    {
        var grid = RenderGrid(FetchOf(GridPages.Stretched(("Name", 1))).Invoke);

        Assert.Equal($"{Fixed} minmax(min-content, 1fr) {Fixed} min-content", Tracks(grid));
    }

    [Fact]
    public void Weights_SplitTheFreeWidth()
    {
        var grid = RenderGrid(FetchOf(GridPages.Stretched(("Id", 1), ("Name", 2))).Invoke);

        Assert.Equal($"minmax(min-content, 1fr) minmax(min-content, 2fr) {Fixed} min-content", Tracks(grid));
    }

    [Fact]
    public void SelectColumn_HasItsTrack_BeforeTheActionsColumn()
    {
        var grid = RenderGrid(FetchOf(GridPages.Stretched(("Name", 1))).Invoke, With(select: true));

        Assert.Equal($"{Fixed} minmax(min-content, 1fr) {Fixed} {Fixed} min-content", Tracks(grid));
    }

    [Fact]
    public void StretchColumns_ReplacesTheColumnMarks()
    {
        var grid = RenderGrid(FetchOf(GridPages.Stretched(("Name", 1))).Invoke, With(new Dictionary<string, int> { ["Note"] = 3 }));

        Assert.Equal($"{Fixed} {Fixed} minmax(min-content, 3fr) min-content", Tracks(grid));
    }

    [Fact]
    public void EmptyStretchColumns_LetsTheActionsColumnFill()
    {
        var grid = RenderGrid(FetchOf(GridPages.Stretched(("Name", 1))).Invoke, With(new Dictionary<string, int>()));

        Assert.Equal($"{Fixed} {Fixed} {Fixed} {Fill}", Tracks(grid));
    }

    [Fact]
    public void StretchColumns_IgnoresUnknownKeys()
    {
        var grid = RenderGrid(FetchOf(GridPages.Columns).Invoke, With(new Dictionary<string, int> { ["Missing"] = 2, ["Name"] = 1 }));

        Assert.Equal($"{Fixed} minmax(min-content, 1fr) {Fixed} min-content", Tracks(grid));
    }

    [Fact]
    public void StretchColumns_UsesTheDictionarysComparer()
    {
        var stretch = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["name"] = 1 };

        var grid = RenderGrid(FetchOf(GridPages.Columns).Invoke, With(stretch));

        Assert.Equal($"{Fixed} minmax(min-content, 1fr) {Fixed} min-content", Tracks(grid));
    }

    [Fact]
    public void StretchWeightBelowOne_Throws()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => RenderGrid(FetchOf(GridPages.Columns).Invoke, With(new Dictionary<string, int> { ["Name"] = 0 })));

        Assert.Equal("StretchColumns", error.ParamName);
    }

    [Fact]
    public void NewStretchColumns_ApplyWithoutAFetch()
    {
        var fetch = FetchOf(GridPages.Columns);
        var grid = RenderGrid(fetch.Invoke, With());

        grid.Render(b => b.Add(x => x.Parameters, With(new Dictionary<string, int> { ["Name"] = 1 })));

        Assert.Equal($"{Fixed} minmax(min-content, 1fr) {Fixed} min-content", Tracks(grid));
        Assert.Single(fetch.Queries);
    }

    [Fact]
    public void UserHiddenStretchingColumn_HasNoTrack()
    {
        var grid = RenderGrid(FetchOf(GridPages.Stretched(("Id", 1), ("Name", 2))).Invoke);

        grid.InvokeAsync(() => grid.Instance.SetColumnVisible("Id", false));

        Assert.Equal($"minmax(min-content, 2fr) {Fixed} min-content", Tracks(grid));
    }

    [Fact]
    public void AllMarkedColumnsHidden_LetTheActionsColumnFill()
    {
        var grid = RenderGrid(FetchOf(GridPages.Stretched(("Name", 1))).Invoke);

        grid.InvokeAsync(() => grid.Instance.SetColumnVisible("Name", false));

        Assert.Equal($"{Fixed} {Fixed} {Fill}", Tracks(grid));
    }

    [Fact]
    public void StretchColumnLeftOutOfThePage_LetsTheActionsColumnFill()
    {
        // An access policy that hides Name leaves it out of the page; the grid's override still names it.
        IReadOnlyList<MaviGridColumn> columns =
        [
            new() { Key = "Id", Title = "Id", Index = 0, Sequence = 1, PropertyType = MaviGridPropertyType.Int },
            new() { Key = "Note", Title = "Note", Index = 1, Sequence = 3, PropertyType = MaviGridPropertyType.String }
        ];

        var grid = RenderGrid(FetchOf(columns).Invoke, With(new Dictionary<string, int> { ["Name"] = 1 }));

        Assert.Equal($"{Fixed} {Fixed} {Fill}", Tracks(grid));
    }

    [Fact]
    public void NextPage_KeepsTheStretchedTracks()
    {
        var columns = GridPages.Stretched(("Name", 1));
        var fetch = new RecordingFetch((q, _) => GridPages.PageWith(columns, q, 30, [.. Enumerable.Range((q.PageNumber - 1) * q.PageSize + 1, q.PageSize)]));
        var grid = RenderGrid(fetch.Invoke);

        grid.Find("[data-page-next] button").Click();

        Assert.Equal(2, fetch.Queries.Count);
        Assert.Equal($"{Fixed} minmax(min-content, 1fr) {Fixed} min-content", Tracks(grid));
    }
}

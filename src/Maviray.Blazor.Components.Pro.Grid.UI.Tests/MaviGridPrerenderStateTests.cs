using Bunit;
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests;

/// <summary>The page fetched while prerendering, carried into the interactive grid so it isn't fetched twice.</summary>
public class MaviGridPrerenderStateTests : MaviGridTestContext
{
    private void Prerender(RecordingFetch fetch, string? key = "orders", MaviGridParameters? parameters = null,
        Action<ComponentParameterCollectionBuilder<MaviGrid<int>>>? configure = null)
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderGrid(fetch.Invoke, parameters, b => { b.Add(x => x.PrerenderedPageKey, key); configure?.Invoke(b); });
        PersistentState.TriggerOnPersisting();
    }

    private IRenderedComponent<MaviGrid<int>> StartInteractive(RecordingFetch fetch, string? key = "orders", MaviGridParameters? parameters = null,
        Action<ComponentParameterCollectionBuilder<MaviGrid<int>>>? configure = null)
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        return RenderGrid(fetch.Invoke, parameters, b => { b.Add(x => x.PrerenderedPageKey, key); configure?.Invoke(b); });
    }

    [Fact]
    public void InteractiveGrid_StartsFromThePrerenderedPage_WithoutFetching()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.WithCursors(q, 30));
        Prerender(fetch);

        var grid = StartInteractive(fetch);

        Assert.Single(fetch.Queries);
        Assert.Equal(Enumerable.Range(1, 10).Select(k => k.ToString()), RowKeys(grid));
        Assert.Contains("1–10 of 30", grid.Find("[data-pager]").TextContent);
    }

    [Fact]
    public void RestoredPage_GivesTheNextFetchItsTotalAndCursor()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.WithCursors(q, 30));
        Prerender(fetch);
        var grid = StartInteractive(fetch);

        grid.Find("[data-page-next] button").Click();

        Assert.Equal(30, fetch.Queries[^1].KnownTotalRows);
        Assert.Equal(new MaviGridCursor("after-10", MaviGridCursorDirection.Next), fetch.Queries[^1].Cursor);
    }

    [Fact]
    public void RestoredPage_KeepsItsStretchingColumns()
    {
        var columns = GridPages.Stretched(("Name", 1));
        var fetch = new RecordingFetch((q, _) => GridPages.PageWith(columns, q, 1, 1));
        Prerender(fetch);

        var grid = StartInteractive(fetch);

        Assert.Single(fetch.Queries);
        Assert.Contains("minmax(min-content, 1fr) minmax(min-content, 200px) min-content", grid.Find("[data-grid-body]").GetAttribute("style"));
    }

    [Fact]
    public void WithoutAKey_TheInteractiveGridFetchesAgain()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.WithCursors(q, 30));
        Prerender(fetch, key: null);

        StartInteractive(fetch, key: null);

        Assert.Equal(2, fetch.Queries.Count);
    }

    [Fact]
    public void AnotherKey_FetchesAgain()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.WithCursors(q, 30));
        Prerender(fetch, key: "orders");

        StartInteractive(fetch, key: "customers");

        Assert.Equal(2, fetch.Queries.Count);
    }

    [Fact]
    public void ADifferentInitialQuery_FetchesAgain()
    {
        var fetch = new RecordingFetch((q, _) => GridPages.WithCursors(q, 30));
        Prerender(fetch, parameters: new MaviGridParameters { SortColumnKey = "Name" });

        StartInteractive(fetch, parameters: new MaviGridParameters { SortColumnKey = "Id" });

        Assert.Equal(2, fetch.Queries.Count);
        Assert.Equal("Id", fetch.Queries[^1].SortColumnKey);
    }

    [Fact]
    public void AFailedPrerenderFetch_IsNotCarriedOver()
    {
        var fetch = new RecordingFetch((q, call) => call == 1 ? throw new InvalidOperationException() : GridPages.Page(q, 4));
        Prerender(fetch);

        var grid = StartInteractive(fetch);

        Assert.Equal(2, fetch.Queries.Count);
        Assert.Equal(["4"], RowKeys(grid));
    }

    [Fact]
    public void ADeniedPrerenderedPage_IsCarriedOver()
    {
        var fetch = new RecordingFetch((_, _) => GridPages.Denied("Ask your manager"));
        Prerender(fetch);

        var grid = StartInteractive(fetch);

        Assert.Single(fetch.Queries);
        Assert.Equal("Ask your manager", grid.Find("[data-grid-state='access-denied']").TextContent.Trim());
    }

    [Fact]
    public void RestoredRowActions_AreRaisedWithTheirValues()
    {
        static MaviGridCollection<int> WithAction(MaviGridQuery q)
        {
            var page = GridPages.Page(q, 1);
            return new MaviGridCollection<int>
            {
                Columns = page.Columns,
                Rows = [new MaviGridRow<int> { Key = 1, Cells = page.Rows[0].Cells, ContextActions = [new MaviRowContextMenuItem<int> { Key = 1, Title = "Edit", Icon = "lni lni-pencil-1", ThemeColorScheme = ThemeColorScheme.Alert }] }],
                PageNumber = 1,
                PageSize = q.PageSize,
                TotalRows = 1,
                TotalPages = 1
            };
        }

        IMaviRowContextMenuItem<int>? clicked = null;
        var fetch = new RecordingFetch((q, _) => WithAction(q));
        var parameters = new MaviGridParameters { TableRowContextMenuDisplayStyle = TableRowContextMenuDisplayStyle.Icons };
        Prerender(fetch, parameters: parameters);

        var grid = StartInteractive(fetch, parameters: parameters, configure: b => b.Add(x => x.OnRowContextMenuClick, (IMaviRowContextMenuItem<int> item) => clicked = item));
        grid.Find("[data-row-key='1'] [data-row-actions] button").Click();

        Assert.Single(fetch.Queries);
        Assert.Equal((1, "Edit", ThemeColorScheme.Alert), (clicked!.Key, clicked.Title, clicked.ThemeColorScheme));
    }

    [Fact]
    public void RestoredGrid_StillBuildsItsMainMenu()
    {
        var fetch = new RecordingFetch(1);
        Action<ComponentParameterCollectionBuilder<MaviGrid<int>>> menu = b => b.Add(
            x => x.ConstructMainContextMenu,
            () => Task.FromResult<IEnumerable<Core.Interfaces.IMaviContextMenuItem>>([new MaviContextMenuItem { Title = "Export" }]));
        Prerender(fetch, configure: menu);

        var grid = StartInteractive(fetch, configure: menu);

        Assert.Single(fetch.Queries);
        Assert.Contains("Export", grid.Markup);
    }
}

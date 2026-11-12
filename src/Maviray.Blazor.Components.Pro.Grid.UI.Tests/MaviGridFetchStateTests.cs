using Bunit;
using Maviray.Blazor.Components.Pro.Core.Models;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests;

/// <summary>Fetch errors, cancellation, the access-denied state and prerendering.</summary>
public class MaviGridFetchStateTests : MaviGridTestContext
{
    private readonly List<Exception> _errors = [];

    private IRenderedComponent<MaviGrid<int>> RenderReportingGrid(
        Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<int>>> fetch,
        MaviGridParameters? parameters = null,
        Action<ComponentParameterCollectionBuilder<MaviGrid<int>>>? configure = null) =>
        RenderGrid(fetch, parameters, b =>
        {
            b.Add(x => x.OnFetchError, (Exception e) => _errors.Add(e));
            configure?.Invoke(b);
        });

    // Fetch errors

    [Fact]
    public void FetchFailure_ShowsTheErrorState_AndRaisesOnFetchError()
    {
        var boom = new InvalidOperationException("boom");
        var grid = RenderReportingGrid((_, _) => Task.FromException<MaviGridCollection<int>>(boom));

        Assert.Contains("Data could not be loaded", grid.Find("[data-grid-state='fetch-error']").TextContent);
        Assert.Equal("alert", grid.Find("[data-grid-state='fetch-error']").GetAttribute("role"));
        Assert.Same(boom, Assert.Single(_errors));
        Assert.Empty(grid.FindAll("[data-grid-state='no-data']"));
    }

    [Fact]
    public void FetchErrorContent_ReplacesTheDefaultText()
    {
        var grid = RenderGrid((_, _) => Task.FromException<MaviGridCollection<int>>(new InvalidOperationException()),
            configure: b => b.Add(x => x.FetchErrorContent, (RenderFragment)(r => r.AddMarkupContent(0, "<b id=\"custom\">x</b>"))));

        Assert.NotNull(grid.Find("[data-grid-state='fetch-error'] #custom"));
        Assert.DoesNotContain("Data could not be loaded", grid.Markup);
    }

    [Fact]
    public async Task FailureAfterSuccess_ClearsThePreviousRows()
    {
        var fetch = new RecordingFetch((q, call) => call == 1 ? GridPages.PageOf(q, 1, 2) : throw new InvalidOperationException());
        var grid = RenderReportingGrid(fetch.Invoke);

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Empty(RowKeys(grid));
        Assert.NotNull(grid.Find("[data-grid-state='fetch-error']"));
    }

    [Fact]
    public async Task SuccessAfterFailure_ClearsTheErrorState()
    {
        var fetch = new RecordingFetch((q, call) => call == 1 ? throw new InvalidOperationException() : GridPages.Page(q, 5));
        var grid = RenderReportingGrid(fetch.Invoke);

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Empty(grid.FindAll("[data-grid-state='fetch-error']"));
        Assert.Equal(["5"], RowKeys(grid));
    }

    [Fact]
    public void CancellationNotCausedByTheGrid_IsReportedAsAnError()
    {
        var grid = RenderReportingGrid((_, _) => Task.FromException<MaviGridCollection<int>>(new OperationCanceledException()));

        Assert.NotNull(grid.Find("[data-grid-state='fetch-error']"));
        Assert.Single(_errors);
    }

    // Cancellation

    [Fact]
    public async Task EachFetch_GetsItsOwnLiveToken()
    {
        var fetch = new RecordingFetch(1);
        var grid = RenderGrid(fetch.Invoke);

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.NotEqual(fetch.Tokens[0], fetch.Tokens[1]);
        Assert.True(fetch.Tokens[1].CanBeCanceled);
    }

    [Fact]
    public async Task SupersededFetch_IsCancelled_AndNotReported()
    {
        var tokens = new List<CancellationToken>();
        var grid = RenderReportingGrid(async (q, ct) =>
        {
            tokens.Add(ct);

            if (tokens.Count == 1)
            {
                await Task.Delay(Timeout.Infinite, ct);
            }

            return GridPages.Page(q, 7);
        });

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        grid.WaitForAssertion(() => Assert.Equal(["7"], RowKeys(grid)));
        Assert.True(tokens[0].IsCancellationRequested);
        Assert.Empty(_errors);
        Assert.Empty(grid.FindAll("[data-grid-state='fetch-error']"));
    }

    [Fact]
    public async Task StaleFetchFinishingLate_DoesNotReplaceNewerRows()
    {
        var gate = new TaskCompletionSource();
        var calls = 0;
        var grid = RenderGrid(async (q, _) =>
        {
            if (++calls == 1)
            {
                await gate.Task;
                return GridPages.Page(q, 9);
            }

            return GridPages.Page(q, 1);
        });

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());
        await grid.InvokeAsync(gate.SetResult);

        Assert.Equal(["1"], RowKeys(grid));
    }

    [Fact]
    public async Task DisposingDuringAFetch_CancelsIt_WithoutReportingAnError()
    {
        var tokens = new List<CancellationToken>();
        RenderReportingGrid(async (q, ct) =>
        {
            tokens.Add(ct);
            await Task.Delay(Timeout.Infinite, ct);
            return GridPages.Page(q);
        });

        await DisposeComponentsAsync();

        Assert.True(tokens[0].IsCancellationRequested);
        Assert.Empty(_errors);
    }

    // Access denied

    [Fact]
    public void DeniedPage_ShowsTheDeniedState_WithHeadersButNoFilterRowSelectColumnOrPager()
    {
        var parameters = new MaviGridParameters { EnableFilters = true, DisplaySelectColumn = true };
        var grid = RenderGrid((_, _) => Task.FromResult(GridPages.Denied()), parameters);

        var denied = grid.Find("[data-grid-state='access-denied']");
        Assert.Equal("You do not have access to this data", denied.TextContent.Trim());
        Assert.NotEqual(parameters.NoDataAvailableTitle, denied.TextContent.Trim());
        Assert.Equal("alert", denied.GetAttribute("role"));
        Assert.NotEmpty(grid.FindAll("[data-column-header]"));
        Assert.Empty(grid.FindAll("input[data-filter-key]"));
        Assert.Empty(grid.FindAll("[data-pager]"));
        Assert.Empty(grid.FindAll("[data-select-header]"));
        Assert.Empty(grid.FindAll("[data-grid-state='no-data']"));
    }

    [Fact]
    public void DeniedMessage_ReplacesTheTitle()
    {
        var grid = RenderGrid((_, _) => Task.FromResult(GridPages.Denied("Ask your manager")));

        Assert.Equal("Ask your manager", grid.Find("[data-grid-state='access-denied']").TextContent.Trim());
    }

    [Fact]
    public void AccessDeniedContent_ReplacesTheMessage()
    {
        var grid = RenderGrid((_, _) => Task.FromResult(GridPages.Denied("m")),
            configure: b => b.Add(x => x.AccessDeniedContent, (RenderFragment)(r => r.AddMarkupContent(0, "<b id=\"custom\">x</b>"))));

        var denied = grid.Find("[data-grid-state='access-denied']");
        Assert.NotNull(denied.QuerySelector("#custom"));
        Assert.DoesNotContain(">m<", denied.InnerHtml);
    }

    [Fact]
    public void DeniedPage_KeepsTheMainMenu()
    {
        var grid = RenderGrid((_, _) => Task.FromResult(GridPages.Denied()), configure: b => b.Add(
            x => x.ConstructMainContextMenu,
            () => Task.FromResult<IEnumerable<Core.Interfaces.IMaviContextMenuItem>>([new MaviContextMenuItem { Title = "Request access" }])));

        Assert.NotNull(grid.Find("[data-main-menu-toggle]"));
        Assert.Contains("Request access", grid.Markup);
    }

    [Fact]
    public async Task AllowedFetchAfterDenial_RestoresRowsFilterRowAndPager()
    {
        var fetch = new RecordingFetch((q, call) => call == 1 ? GridPages.Denied() : GridPages.Page(q, 1));
        var grid = RenderGrid(fetch.Invoke, new MaviGridParameters { EnableFilters = true });

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Empty(grid.FindAll("[data-grid-state='access-denied']"));
        Assert.Equal(["1"], RowKeys(grid));
        Assert.NotEmpty(grid.FindAll("input[data-filter-key]"));
        Assert.NotNull(grid.Find("[data-pager]"));
    }

    // Prerendering

    [Fact]
    public void Prerender_WithFetchingDisabled_DoesNotFetch_AndShowsAPlaceholder()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var fetch = new RecordingFetch(1);

        var grid = RenderGrid(fetch.Invoke, new MaviGridParameters { FetchWhilePrerendering = false });

        Assert.Empty(fetch.Queries);
        Assert.Equal("status", grid.Find("[data-grid-state='prerender']").GetAttribute("role"));
    }

    [Fact]
    public void Prerender_WithFetchingDisabled_ShowsPrerenderContent()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var grid = RenderGrid(new RecordingFetch(1).Invoke, new MaviGridParameters { FetchWhilePrerendering = false },
            b => b.Add(x => x.PrerenderContent, (RenderFragment)(r => r.AddMarkupContent(0, "<b id=\"loading\">…</b>"))));

        Assert.NotNull(grid.Find("[data-grid-state='prerender'] #loading"));
    }

    [Fact]
    public void Interactive_WithPrerenderFetchingDisabled_FetchesNormally()
    {
        var fetch = new RecordingFetch(1);

        var grid = RenderGrid(fetch.Invoke, new MaviGridParameters { FetchWhilePrerendering = false });

        Assert.Single(fetch.Queries);
        Assert.Empty(grid.FindAll("[data-grid-state='prerender']"));
    }

    [Fact]
    public void Prerender_ByDefault_Fetches()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var fetch = new RecordingFetch(1);

        RenderGrid(fetch.Invoke);

        Assert.Single(fetch.Queries);
    }

    // Review findings

    [Fact]
    public async Task DuplicateRowKeys_RenderAndRerenderWithoutThrowing()
    {
        var grid = RenderGrid((q, _) => Task.FromResult(GridPages.Page(q, 1, 1)));

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Equal(["1", "1"], RowKeys(grid));
    }

    [Fact]
    public void MainMenuBuilderFailure_IsNotReportedAsAFetchError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => RenderReportingGrid(new RecordingFetch(1).Invoke, null, b => b.Add(
            x => x.ConstructMainContextMenu,
            () => Task.FromException<IEnumerable<Core.Interfaces.IMaviContextMenuItem>>(new InvalidOperationException("menu bug")))));

        Assert.Equal("menu bug", ex.Message);
        Assert.Empty(_errors);
    }

    [Fact]
    public async Task FilterTypedBeforeADenial_IsAppliedAgainWhenAccessReturns()
    {
        var denied = false;
        var fetch = new RecordingFetch((q, _) => denied ? GridPages.Denied() : GridPages.Page(q, 1));
        var grid = RenderGrid(fetch.Invoke, new MaviGridParameters { EnableFilters = true, FilterDebounceMilliseconds = 0 });

        grid.Find("input[data-filter-key='Name']").Input("Ab");
        grid.WaitForAssertion(() => Assert.NotEmpty(fetch.Queries[^1].Filters));

        denied = true;
        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());
        denied = false;
        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Equal([new MaviGridColumnFilter("Name", Core.Enums.Grid.MaviGridFilterCriteria.Contains, "Ab")], fetch.Queries[^1].Filters);
        Assert.Equal("Ab", grid.Find("input[data-filter-key='Name']").GetAttribute("value"));
    }

    [Fact]
    public async Task OutsideClickAfterDisposal_IsIgnored()
    {
        var grid = RenderGrid(new RecordingFetch(1).Invoke, configure: b => b.Add(
            x => x.ConstructMainContextMenu,
            () => Task.FromResult<IEnumerable<Core.Interfaces.IMaviContextMenuItem>>([new MaviContextMenuItem { Title = "Export" }])));
        grid.Find("[data-main-menu-toggle] button").Click();
        var instance = grid.Instance;

        await DisposeComponentsAsync();

        // JS interop calls back on the renderer's dispatcher.
        Assert.Null(await Record.ExceptionAsync(() => Renderer.Dispatcher.InvokeAsync(() => instance.HandleOutsideClick("elsewhere"))));
    }

    [Fact]
    public async Task RefreshSupersededByANewerFetch_StillRebuildsTheMainMenu()
    {
        var menuTitle = "Menu v1";
        var gate = new TaskCompletionSource();
        var calls = 0;
        var grid = RenderGrid(
            async (q, _) =>
            {
                // The refresh (call 2) ignores its token and finishes after the newer fetch, as a slow query would.
                if (++calls == 2)
                {
                    await gate.Task;
                }

                return GridPages.PageOf(q, 30, 1);
            },
            configure: b => b.Add(
                x => x.ConstructMainContextMenu,
                () => Task.FromResult<IEnumerable<Core.Interfaces.IMaviContextMenuItem>>([new MaviContextMenuItem { Title = menuTitle }])));

        menuTitle = "Menu v2";
        var refresh = grid.InvokeAsync(() => grid.Instance.RefreshAsync());
        grid.Find("[data-sort-key='Name']").Click();
        await grid.InvokeAsync(gate.SetResult);
        await refresh;

        Assert.Equal(3, calls);
        Assert.Contains("Menu v2", grid.Markup);
    }

    [Fact]
    public async Task FetchRequestedAfterDisposal_DoesNotCallFetchData()
    {
        var fetch = new RecordingFetch(1);
        var grid = RenderGrid(fetch.Invoke);
        var instance = grid.Instance;

        await DisposeComponentsAsync();
        await Renderer.Dispatcher.InvokeAsync(instance.RefreshAsync);

        Assert.Single(fetch.Queries);
    }
}

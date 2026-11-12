using Bunit;
using Bunit.TestDoubles;
using Maviray.Blazor.Components.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;

public abstract class MaviGridTestContext : BunitContext
{
    protected MaviGridTestContext()
    {
        // Registered before anything is resolved; only grids with a PrerenderedPageKey use it.
        PersistentState = AddBunitPersistentComponentState();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMaviComponents();
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    protected BunitPersistentComponentState PersistentState { get; }

    protected static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    protected IRenderedComponent<MaviGrid<int>> RenderGrid(
        Func<MaviGridQuery, CancellationToken, Task<MaviGridCollection<int>>> fetch,
        MaviGridParameters? parameters = null,
        Action<ComponentParameterCollectionBuilder<MaviGrid<int>>>? configure = null) =>
        Render<MaviGrid<int>>(builder =>
        {
            builder.Add(x => x.FetchData, fetch);
            builder.Add(x => x.Parameters, parameters ?? new MaviGridParameters { FilterDebounceMilliseconds = 0 });
            configure?.Invoke(builder);
        });

    protected static IEnumerable<string?> RowKeys(IRenderedComponent<MaviGrid<int>> grid) =>
        grid.FindAll("[data-row-key]").Select(e => e.GetAttribute("data-row-key"));
}

using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Components;

public class MaviComponentProBase : ComponentBase
{
    protected bool EnableLifeCycleLogging { get; private set; }

    private ILogger? _logger;

    [Inject] private ILoggerFactory? LoggerFactory { get; set; }

    [Inject] protected IMaviComponentProOptions? ComponentOptions { get; set; }

    protected ILogger? Logger => _logger ??= LoggerFactory?.CreateLogger(GetType());

    [Parameter] public virtual string? Id { get; set; } = System.Guid.NewGuid().ToString();

    [Parameter] public string? Class { get; set; }

    [Parameter] public string? Style { get; set; }

    [Parameter] public string? Title { get; set; }

    protected bool HasRendered { get; private set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ComponentOptions != null)
        {
            EnableLifeCycleLogging = ComponentOptions.EnableLifecycleLogging;
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (EnableLifeCycleLogging)
        {
            Logger?.LogDebugLifeCycle(Id, GetType());
        }

        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            HasRendered = true;
        }
    }
}
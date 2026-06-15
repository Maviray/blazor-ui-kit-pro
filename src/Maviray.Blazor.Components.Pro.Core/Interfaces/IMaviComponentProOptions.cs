using Microsoft.Extensions.Logging;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces;

public interface IMaviComponentProOptions
{
    /// <summary>
    /// Gets the logging level for component operations.
    /// </summary>
    LogLevel ComponentLogLevel { get; }

    /// <summary>
    /// Gets whether to enable detailed component lifecycle logging.
    /// </summary>
    bool EnableLifecycleLogging { get; }

    /// <summary>
    /// Gets whether to enable performance tracking.
    /// </summary>
    bool EnablePerformanceTracking { get; }
}
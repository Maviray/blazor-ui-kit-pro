using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Maviray.Blazor.Components.Pro.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the grid data factory (scoped) and its options. Chain the returned builder to add a data source,
    /// access policies, fetch observers and global filters.
    /// </summary>
    public static MaviGridBuilder AddMaviGrid(this IServiceCollection services, Action<GridDataFactoryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<GridDataFactoryOptions>()
            .Validate(o => o.MaxPageSize >= 1, $"{nameof(GridDataFactoryOptions.MaxPageSize)} must be at least 1.");

        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.TryAdd(ServiceDescriptor.Scoped(typeof(IGridDataFactory<,,>), typeof(GridDataFactory<,,>)));

        return new MaviGridBuilder(services);
    }
}

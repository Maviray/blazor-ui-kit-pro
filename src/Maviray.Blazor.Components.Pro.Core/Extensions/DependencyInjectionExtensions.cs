using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection RegisterGridDataFactory(this IServiceCollection services)
    {
        services.AddTransient(
            typeof(IGenericGridDataFactory<,,>),
            typeof(GenericGridDataFactory<,,>));
        return services;
    }

    /// <summary>
    /// Registers a stateless, pre-built global grid filter (for example a soft-delete exclusion) that
    /// the data factory applies to every grid query for every entity type. May be called multiple
    /// times; all registered providers are AND-combined.
    /// </summary>
    public static IServiceCollection AddGridGlobalFilter(this IServiceCollection services, IGridGlobalFilterProvider provider)
    {
        services.AddSingleton(provider);
        return services;
    }

    /// <summary>
    /// Registers a global grid filter provider by type so it can take dependencies (for example the
    /// current user or tenant). Defaults to a scoped lifetime, which suits per-request/per-circuit
    /// context. May be called multiple times; all registered providers are AND-combined.
    /// </summary>
    public static IServiceCollection AddGridGlobalFilter<TProvider>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TProvider : class, IGridGlobalFilterProvider
    {
        services.Add(new ServiceDescriptor(typeof(IGridGlobalFilterProvider), typeof(TProvider), lifetime));
        return services;
    }
}
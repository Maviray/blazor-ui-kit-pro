using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

/// <summary>
/// Adds grid services. Registrations by type default to a scoped lifetime (one per circuit in Blazor Server) and are
/// added once even if repeated; registrations by instance are singletons.
/// </summary>
public sealed class MaviGridBuilder
{
    internal MaviGridBuilder(IServiceCollection services) => Services = services;

    public IServiceCollection Services { get; }

    /// <summary>Uses an open generic data source type, such as <c>typeof(MyGridDataSource&lt;&gt;)</c>, for every data item type.</summary>
    public MaviGridBuilder AddDataSource(Type openGenericDataSourceType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(openGenericDataSourceType);
        Services.TryAdd(ServiceDescriptor.Describe(typeof(IGridDataSource<>), openGenericDataSourceType, lifetime));
        return this;
    }

    public MaviGridBuilder AddAccessPolicy<TPolicy>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TPolicy : class, IGridAccessPolicy =>
        AddByType<IGridAccessPolicy, TPolicy>(lifetime);

    public MaviGridBuilder AddAccessPolicy(IGridAccessPolicy policy) => AddInstance(policy);

    public MaviGridBuilder AddFetchObserver<TObserver>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TObserver : class, IGridFetchObserver =>
        AddByType<IGridFetchObserver, TObserver>(lifetime);

    public MaviGridBuilder AddFetchObserver(IGridFetchObserver observer) => AddInstance(observer);

    public MaviGridBuilder AddGlobalFilter<TFilter>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TFilter : class, IGridGlobalFilterProvider =>
        AddByType<IGridGlobalFilterProvider, TFilter>(lifetime);

    public MaviGridBuilder AddGlobalFilter(IGridGlobalFilterProvider filter) => AddInstance(filter);

    private MaviGridBuilder AddByType<TService, TImplementation>(ServiceLifetime lifetime)
        where TService : class
        where TImplementation : class, TService
    {
        Services.TryAddEnumerable(ServiceDescriptor.Describe(typeof(TService), typeof(TImplementation), lifetime));
        return this;
    }

    private MaviGridBuilder AddInstance<TService>(TService instance) where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        Services.AddSingleton(instance);
        return this;
    }
}

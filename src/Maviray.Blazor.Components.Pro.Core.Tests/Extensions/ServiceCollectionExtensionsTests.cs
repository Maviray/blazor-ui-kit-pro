using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private sealed class AllowAllPolicy : IGridAccessPolicy
    {
        public ValueTask<GridAccessDecision<TDataItem>> EvaluateAsync<TDataItem>(GridAccessRequest request, CancellationToken cancellationToken)
            where TDataItem : class => ValueTask.FromResult(GridAccessDecision<TDataItem>.Allow());
    }

    private sealed class NoOpObserver : IGridFetchObserver;

    [Fact]
    public void AddMaviGrid_RegistersAScopedFactory()
    {
        var services = new ServiceCollection();

        services.AddMaviGrid();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IGridDataFactory<,,>));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddMaviGrid_Configure_BindsTheOptions()
    {
        var services = new ServiceCollection();
        services.AddMaviGrid(o => o.RequireExplicitAllow = true);

        using var provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<GridDataFactoryOptions>>().Value.RequireExplicitAllow);
    }

    [Fact]
    public void AddMaviGrid_MaxPageSizeBelowOne_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddMaviGrid(o => o.MaxPageSize = 0);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<GridDataFactoryOptions>>().Value);
    }

    [Fact]
    public void Builder_RegistrationsByType_AreScopedAndRegisteredOnce()
    {
        var services = new ServiceCollection();

        services.AddMaviGrid()
            .AddAccessPolicy<AllowAllPolicy>()
            .AddAccessPolicy<AllowAllPolicy>()
            .AddFetchObserver<NoOpObserver>()
            .AddGlobalFilter<RecordingGlobalFilter>();

        var policy = Assert.Single(services, d => d.ServiceType == typeof(IGridAccessPolicy));
        Assert.Equal(ServiceLifetime.Scoped, policy.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, d => d.ServiceType == typeof(IGridFetchObserver)).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, d => d.ServiceType == typeof(IGridGlobalFilterProvider)).Lifetime);
    }

    [Fact]
    public void Builder_RegistrationsByInstance_AreSingletons_AndKeepEveryInstance()
    {
        var services = new ServiceCollection();
        var first = new RecordingGlobalFilter();
        var second = new RecordingGlobalFilter();

        services.AddMaviGrid().AddGlobalFilter(first).AddGlobalFilter(second).AddAccessPolicy(new AllowAllPolicy());

        var filters = services.Where(d => d.ServiceType == typeof(IGridGlobalFilterProvider)).ToList();
        Assert.Equal([first, second], filters.Select(d => d.ImplementationInstance));
        Assert.All(filters, d => Assert.Equal(ServiceLifetime.Singleton, d.Lifetime));
        Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, d => d.ServiceType == typeof(IGridAccessPolicy)).Lifetime);
    }

    [Fact]
    public async Task AddMaviGrid_ResolvesAWorkingFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IGridDataSource<TestOrder>>(new FakeGridDataSource(TestFactory.Orders(2)));
        services.AddMaviGrid().AddAccessPolicy<AllowAllPolicy>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        var factory = scope.ServiceProvider.GetRequiredService<IGridDataFactory<TestOrder, TestOrderGridModel, int>>();
        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, page.TotalRows);
    }
}

using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests;

/// <summary>A policy on a base entity governs a grid over a derived entity, and EF Core translates its row filter.</summary>
public sealed class InheritedEntityAccessTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _db = new();
    private readonly ServiceProvider _services;

    public InheritedEntityAccessTests()
    {
        _db.Seed(context =>
        {
            context.Documents.Add(new TestDocument { Id = 1, OwnerId = 7, Title = "Memo" });
            context.Invoices.AddRange(
                new TestInvoice { Id = 2, OwnerId = 7, Title = "Invoice A", Amount = 10 },
                new TestInvoice { Id = 3, OwnerId = 8, Title = "Invoice B", Amount = 20 },
                new TestInvoice { Id = 4, OwnerId = 7, Title = "Invoice C", Amount = 30 });
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<TestDbContext>(o => o.UseSqlite(_db.Connection).AddInterceptors(_db.Recorder));
        services.AddMaviGrid(o => o.RequireExplicitAllow = true)
            .AddEntityFrameworkDataSource<TestDbContext>()
            .AddAccessPolicy(new OwnerSevenDocumentsPolicy());
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _db.DisposeAsync();
    }

    private sealed class OwnerSevenDocumentsPolicy : GridAccessPolicy<TestDocument>
    {
        protected override ValueTask<GridAccessDecision<TestDocument>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GridAccessDecision<TestDocument>.Allow(x => x.OwnerId == 7));
    }

    [Fact]
    public async Task BaseTypePolicy_FiltersTheDerivedGridInTheDatabase()
    {
        await using var scope = _services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IGridDataFactory<TestInvoice, TestInvoiceGridModel, int>>();

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.False(page.AccessDenied);
        Assert.Equal([2, 4], page.Rows.Select(r => r.Key));
        Assert.Equal(2, page.TotalRows);
        Assert.All(_db.Commands, c => Assert.Contains("OwnerId", c.Sql));
    }
}

using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests;

/// <summary>A projected grid selects only the columns it shows, through EF Core against SQLite.</summary>
public sealed class ProjectionTests : IAsyncDisposable
{
    private static readonly GridFetchOptions<TestShipment, TestShipmentGridModel> Projected = new() { Projection = new GridProjection<TestShipment>() };

    private readonly SqliteTestDatabase _db = new();
    private readonly ServiceProvider _services;

    public ProjectionTests()
    {
        _db.Seed(context => context.Shipments.AddRange(Enumerable.Range(1, 25).Select(id => new TestShipment
        {
            Id = id,
            Reference = $"S{id:00}",
            Notes = new string('n', 500),
            Carrier = id % 5 == 0 ? null : new TestCarrier { Name = $"Carrier {id}", Phone = "555" }
        })));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<TestDbContext>(o => o.UseSqlite(_db.Connection).AddInterceptors(_db.Recorder));
        services.AddMaviGrid().AddEntityFrameworkDataSource<TestDbContext>();
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _db.DisposeAsync();
    }

    private async Task<MaviGridCollection<int>> Fetch(MaviGridQuery query, GridFetchOptions<TestShipment, TestShipmentGridModel>? options)
    {
        await using var scope = _services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IGridDataFactory<TestShipment, TestShipmentGridModel, int>>();
        return await factory.BuildPageAsync(query, options, Ct);
    }

    private string PageSql() => _db.Commands.Single(c => c.Sql.Contains("LIMIT", StringComparison.Ordinal)).Sql;

    [Fact]
    public async Task WithoutAProjection_TheWholeRowsAreSelected()
    {
        await Fetch(new MaviGridQuery(), options: null);

        Assert.Contains("\"Notes\"", PageSql());
        Assert.Contains("\"Phone\"", PageSql());
    }

    [Fact]
    public async Task WithAProjection_OnlyTheShownColumnsAreSelected()
    {
        var page = await Fetch(new MaviGridQuery { PageSize = 5 }, Projected);

        var sql = PageSql();
        Assert.DoesNotContain("\"Notes\"", sql);
        Assert.DoesNotContain("\"Phone\"", sql);
        Assert.Contains("\"Reference\"", sql);
        Assert.Equal(["1", "S01", "Carrier 1"], page.Rows[0].Cells.Select(c => c.Text));
        Assert.Equal(["5", "S05", ""], page.Rows[4].Cells.Select(c => c.Text));
    }

    [Fact]
    public async Task WithAProjection_KeysetPagingStillSeeks()
    {
        var first = await Fetch(new MaviGridQuery { PageSize = 10, SortColumnKey = "Reference" }, Projected);
        _db.ClearCommands();

        var second = await Fetch(new MaviGridQuery
        {
            PageNumber = 2,
            PageSize = 10,
            SortColumnKey = "Reference",
            KnownTotalRows = first.TotalRows,
            Cursor = new MaviGridCursor(first.NextPageCursor!, MaviGridCursorDirection.Next)
        }, Projected);

        var command = Assert.Single(_db.Commands);
        Assert.DoesNotContain("OFFSET", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Notes\"", command.Sql);
        Assert.Equal(Enumerable.Range(11, 10), second.Rows.Select(r => r.Key));
        Assert.NotNull(second.NextPageCursor);
    }
}

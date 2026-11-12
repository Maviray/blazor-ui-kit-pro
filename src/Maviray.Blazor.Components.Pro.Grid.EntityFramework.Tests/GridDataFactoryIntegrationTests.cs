using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests;

/// <summary>The factory and the EF data source together, resolved from DI, against SQLite.</summary>
public sealed class GridDataFactoryIntegrationTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _db = new();
    private readonly ServiceProvider _services;

    public GridDataFactoryIntegrationTests()
    {
        _db.Seed(context => context.Products.AddRange(Enumerable.Range(1, 30).Select(id => new TestProduct { Id = id, Name = $"Zebra-{id}", Category = $"C{id % 3}" })));

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

    private async Task<MaviGridCollection<int>> Fetch(MaviGridQuery query)
    {
        await using var scope = _services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IGridDataFactory<TestProduct, TestProductGridModel, int>>();
        return await factory.BuildPageAsync(query, cancellationToken: Ct);
    }

    [Fact]
    public async Task Paging_LoadsOnlyThePage()
    {
        var page = await Fetch(new MaviGridQuery { PageNumber = 2, PageSize = 10, SortColumnKey = "Id", SortOrder = SortOrder.Ascending });

        Assert.Equal(Enumerable.Range(11, 10), page.Rows.Select(r => r.Key));
        Assert.Equal(30, page.TotalRows);
        Assert.Contains(_db.Commands, c => c.Sql.Contains("LIMIT", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StringFilter_IsSentAsAParameter_NotAsALiteral()
    {
        var page = await Fetch(new MaviGridQuery { Filters = [new MaviGridColumnFilter("Name", MaviGridFilterCriteria.Equals, "Zebra-7")] });

        Assert.Equal([7], page.Rows.Select(r => r.Key));
        Assert.All(_db.Commands, c => Assert.DoesNotContain("Zebra-7", c.Sql));
        Assert.Contains(_db.Commands, c => c.ParameterValues.Contains("Zebra-7"));
    }

    [Fact]
    public async Task NumberFilter_IsSentAsAParameter_NotAsALiteral()
    {
        var page = await Fetch(new MaviGridQuery { Filters = [new MaviGridColumnFilter("Id", MaviGridFilterCriteria.GreaterThan, "27")] });

        Assert.Equal([28, 29, 30], page.Rows.Select(r => r.Key));
        Assert.All(_db.Commands, c => Assert.DoesNotContain("27", c.Sql));
    }

    [Fact]
    public async Task NextPage_WithTheGridsHints_RunsOneSeekQuery_WithNoCountAndNoOffset()
    {
        var first = await Fetch(new MaviGridQuery { PageSize = 10, SortColumnKey = "Category" });
        _db.ClearCommands();

        // What the grid sends when the user clicks "next" on that page.
        var second = await Fetch(new MaviGridQuery
        {
            PageNumber = 2,
            PageSize = 10,
            SortColumnKey = "Category",
            KnownTotalRows = first.TotalRows,
            Cursor = new MaviGridCursor(first.NextPageCursor!, MaviGridCursorDirection.Next)
        });

        var command = Assert.Single(_db.Commands);
        Assert.DoesNotContain("COUNT", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OFFSET", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(10, second.Rows.Count);
        Assert.Empty(first.Rows.Select(r => r.Key).Intersect(second.Rows.Select(r => r.Key)));
        Assert.Equal((2, 30), (second.PageNumber, second.TotalRows));
        Assert.NotNull(second.NextPageCursor);
        Assert.NotNull(second.PreviousPageCursor);
    }

    [Fact]
    public async Task NumberFilterOutOfTheColumnTypesRange_MatchesNoRows()
    {
        var page = await Fetch(new MaviGridQuery { Filters = [new MaviGridColumnFilter("Id", MaviGridFilterCriteria.Equals, "99999999999")] });

        Assert.Empty(page.Rows);
        Assert.Equal(0, page.TotalRows);
    }

    [Fact]
    public async Task ContainsFilter_IsSentAsAParameter_NotAsALiteral()
    {
        var page = await Fetch(new MaviGridQuery { Filters = [new MaviGridColumnFilter("Name", MaviGridFilterCriteria.Contains, "ra-2")] });

        Assert.Equal([2, 20, 21, 22, 23, 24, 25, 26, 27, 28], page.Rows.Select(r => r.Key));
        Assert.All(_db.Commands, c => Assert.DoesNotContain("ra-2", c.Sql));
    }

    [Fact]
    public async Task EmptyResult_RunsOnlyTheCount()
    {
        var page = await Fetch(new MaviGridQuery { Filters = [new MaviGridColumnFilter("Name", MaviGridFilterCriteria.Equals, "missing")] });

        Assert.Empty(page.Rows);
        Assert.Single(_db.Commands);
    }
}

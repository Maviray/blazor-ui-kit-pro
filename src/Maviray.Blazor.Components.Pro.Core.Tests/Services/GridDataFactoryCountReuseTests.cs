using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

/// <summary>Skipping the count when the grid already knows the total, and counting again when that total is stale.</summary>
public class GridDataFactoryCountReuseTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task KnownTotal_SkipsTheCount()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(25));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { PageNumber = 2, PageSize = 10, KnownTotalRows = 25 }, cancellationToken: Ct);

        Assert.Equal(0, source.CountCalls);
        Assert.Equal(Enumerable.Range(11, 10), page.Rows.Select(r => r.Key));
        Assert.Equal(25, page.TotalRows);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task KnownTotal_PageShorterThanItPredicts_CountsAgain()
    {
        // The grid saw 30 rows; 5 were deleted since, so the last page has 5 rows, not 10.
        var source = new FakeGridDataSource(TestFactory.Orders(25));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { PageNumber = 3, PageSize = 10, KnownTotalRows = 30 }, cancellationToken: Ct);

        Assert.Equal(1, source.CountCalls);
        Assert.Equal(25, page.TotalRows);
        Assert.Equal(3, page.PageNumber);
        Assert.Equal(Enumerable.Range(21, 5), page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task KnownTotal_PageNowPastTheEnd_LoadsTheNewLastPage()
    {
        // The grid saw 30 rows; 15 were deleted since, so page 3 no longer exists.
        var source = new FakeGridDataSource(TestFactory.Orders(15));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { PageNumber = 3, PageSize = 10, KnownTotalRows = 30 }, cancellationToken: Ct);

        Assert.Equal(15, page.TotalRows);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(Enumerable.Range(11, 5), page.Rows.Select(r => r.Key));
        Assert.Equal(10, source.LastGetQuery!.Skip);
    }

    [Fact]
    public async Task KnownTotal_EverythingDeleted_ReturnsAnEmptyPage()
    {
        var source = new FakeGridDataSource([]);

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { PageNumber = 2, PageSize = 10, KnownTotalRows = 30 }, cancellationToken: Ct);

        Assert.Equal(0, page.TotalRows);
        Assert.Equal(1, page.PageNumber);
        Assert.Empty(page.Rows);
    }

    [Fact]
    public async Task KnownTotalOfZero_IsCountedAgain()
    {
        // Nothing would be loaded for a zero total, so rows added since could never show up.
        var source = new FakeGridDataSource(TestFactory.Orders(5));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { KnownTotalRows = 0 }, cancellationToken: Ct);

        Assert.Equal(1, source.CountCalls);
        Assert.Equal(5, page.TotalRows);
    }

    [Fact]
    public async Task ReuseTotalRowsOff_AlwaysCounts()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(25));
        var factory = TestFactory.Create(source, options: new GridDataFactoryOptions { ReuseTotalRows = false });

        await factory.BuildPageAsync(new MaviGridQuery { PageNumber = 2, PageSize = 10, KnownTotalRows = 25 }, cancellationToken: Ct);

        Assert.Equal(1, source.CountCalls);
    }
}

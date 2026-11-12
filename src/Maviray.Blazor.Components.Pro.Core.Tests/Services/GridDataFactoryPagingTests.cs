using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GridDataFactoryPagingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BuildPageAsync_LoadsOnlyTheRequestedPage()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(25));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { PageNumber = 2, PageSize = 10 }, cancellationToken: Ct);

        Assert.Equal(10, source.LastGetQuery!.Skip);
        Assert.Equal(10, source.LastGetQuery.Take);
        Assert.Equal(Enumerable.Range(11, 10), page.Rows.Select(r => r.Key));
        Assert.Equal(25, page.TotalRows);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(11, page.FirstRowNumber);
        Assert.Equal(20, page.LastRowNumber);
    }

    [Fact]
    public async Task BuildPageAsync_PageBeyondLast_IsClampedToLastPage()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(25));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { PageNumber = 9, PageSize = 10 }, cancellationToken: Ct);

        Assert.Equal(3, page.PageNumber);
        Assert.Equal(Enumerable.Range(21, 5), page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task BuildPageAsync_PageBelowOne_IsTreatedAsFirstPage()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(25));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { PageNumber = 0, PageSize = 10 }, cancellationToken: Ct);

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(0, source.LastGetQuery!.Skip);
    }

    [Theory]
    [InlineData(1000, 50)]
    [InlineData(0, 1)]
    public async Task BuildPageAsync_PageSize_IsClampedBetweenOneAndMaxPageSize(int requested, int expected)
    {
        var source = new FakeGridDataSource(TestFactory.Orders(120));

        var page = await TestFactory.Create(source, options: new GridDataFactoryOptions { MaxPageSize = 50 })
            .BuildPageAsync(new MaviGridQuery { PageSize = requested }, cancellationToken: Ct);

        Assert.Equal(expected, page.PageSize);
        Assert.Equal(expected, source.LastGetQuery!.Take);
    }

    [Fact]
    public async Task BuildPageAsync_EmptyResult_SkipsThePageQuery_AndKeepsColumns()
    {
        var source = new FakeGridDataSource([]);

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(1, source.CountCalls);
        Assert.Equal(0, source.GetCalls);
        Assert.Empty(page.Rows);
        Assert.Equal(0, page.TotalRows);
        Assert.Equal(0, page.TotalPages);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(0, page.FirstRowNumber);
        Assert.Equal(9, page.Columns.Count);
    }

    [Fact]
    public async Task BuildPageAsync_NoSortColumn_LetsTheDataSourceUseItsDefaultOrder()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(3));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Null(source.LastGetQuery!.Sort);
        Assert.Null(page.SortColumnKey);
    }

    [Fact]
    public async Task BuildPageAsync_SortColumn_IsPassedToTheDataSourceWithItsDirection()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(3));

        var page = await TestFactory.Create(source).BuildPageAsync(
            new MaviGridQuery { SortColumnKey = "Name", SortOrder = SortOrder.Descending }, cancellationToken: Ct);

        Assert.Equal(new GridSort("Name", SortOrder.Descending), source.LastGetQuery!.Sort);
        Assert.Equal([3, 2, 1], page.Rows.Select(r => r.Key));
        Assert.Equal("Name", page.SortColumnKey);
        Assert.Equal(SortOrder.Descending, page.SortOrder);
    }

    [Fact]
    public async Task BuildPageAsync_PassesTheModelNavigationsAsIncludes()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(3));

        await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(["Vendor"], source.LastGetQuery!.IncludePaths);
    }
}

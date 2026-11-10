using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GridDataFactoryFilterTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static MaviGridQuery Filtered(string columnKey, MaviGridFilterCriteria criteria, string value) =>
        new() { Filters = [new MaviGridColumnFilter(columnKey, criteria, value)] };

    [Fact]
    public async Task ColumnFilter_LimitsTheCountAndThePage()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(12));

        var page = await TestFactory.Create(source).BuildPageAsync(Filtered("Name", MaviGridFilterCriteria.Contains, "Order 1"), cancellationToken: Ct);

        Assert.Equal(3, page.TotalRows);
        Assert.Equal([10, 11, 12], page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task FilterOnAPropertyThatIsNotAColumn_IsIgnored()
    {
        var orders = TestFactory.Orders(4);
        orders[0].Secret = "x";
        var source = new FakeGridDataSource(orders);

        var page = await TestFactory.Create(source).BuildPageAsync(Filtered("Secret", MaviGridFilterCriteria.Equals, "x"), cancellationToken: Ct);

        Assert.Equal(4, page.TotalRows);
        Assert.Null(source.LastCountQuery!.Filter);
    }

    [Fact]
    public async Task SortOnAPropertyThatIsNotAColumn_FallsBackToTheDefaultOrder()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(3));

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { SortColumnKey = "Secret" }, cancellationToken: Ct);

        Assert.Null(source.LastGetQuery!.Sort);
        Assert.Null(page.SortColumnKey);
    }

    [Fact]
    public async Task FilterOnAComputedColumnWithoutPath_IsIgnored()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(4));

        var page = await TestFactory.Create(source).BuildPageAsync(Filtered("DisplayLabel", MaviGridFilterCriteria.Contains, "1-"), cancellationToken: Ct);

        Assert.Equal(4, page.TotalRows);
    }

    [Fact]
    public async Task MappedColumnWithNullDefault_FiltersCoalescedValues()
    {
        var source = new FakeGridDataSource(
        [
            new TestOrder { Id = 1, Approved = true },
            new TestOrder { Id = 2, Approved = null },
            new TestOrder { Id = 3, Approved = false }
        ]);
        var options = new GridFetchOptions<TestOrder, TestOrderGridModel>
        {
            PropertyPathMap = new GridPropertyPathMap<TestOrder, TestOrderGridModel>().Map(m => m.ApprovedOrDefault, "Approved", nullDefault: false)
        };

        var page = await TestFactory.Create(source).BuildPageAsync(Filtered("ApprovedOrDefault", MaviGridFilterCriteria.IsFalse, "false"), options, Ct);

        Assert.Equal([2, 3], page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task NestedColumnFilter_UsesTheNavigationPath()
    {
        var source = new FakeGridDataSource(
        [
            new TestOrder { Id = 1, Vendor = new TestVendor { Code = "A" } },
            new TestOrder { Id = 2, Vendor = new TestVendor { Code = "B" } }
        ]);

        var page = await TestFactory.Create(source).BuildPageAsync(Filtered("Vendor.Code", MaviGridFilterCriteria.Equals, "B"), cancellationToken: Ct);

        Assert.Equal([2], page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task OptionsFilter_IsAndCombinedWithColumnFilters()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(12));
        var options = new GridFetchOptions<TestOrder, TestOrderGridModel> { Filter = x => x.Id > 10 };

        var page = await TestFactory.Create(source).BuildPageAsync(Filtered("Name", MaviGridFilterCriteria.Contains, "Order 1"), options, Ct);

        Assert.Equal([11, 12], page.Rows.Select(r => r.Key));
        Assert.Equal(2, page.TotalRows);
    }

    [Fact]
    public async Task GlobalFilters_AreAndCombined()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(12));
        var even = new RecordingGlobalFilter(x => x.Id % 2 == 0);
        var small = new RecordingGlobalFilter(x => x.Id < 7);

        var page = await TestFactory.Create(source, globalFilters: [even, small]).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal([2, 4, 6], page.Rows.Select(r => r.Key));
    }
}

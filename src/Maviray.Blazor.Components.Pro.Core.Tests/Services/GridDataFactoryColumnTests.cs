using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GridDataFactoryColumnTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string Text(MaviGridCollection<int> page, MaviGridRow<int> row, string columnKey) =>
        row.Cells[page.Columns.Single(c => c.Key == columnKey).Index].Text;

    [Fact]
    public async Task Columns_ComeFromTheModel_InSequenceOrder_WithMatchingIndexes()
    {
        var page = await TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(1))).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(
            ["Id", "Name", "VendorCode", "DisplayLabel", "ApprovedOrDefault", "Status", "CreatedOn", "Vendor.IsPreferred", "Vendor.Code"],
            page.Columns.Select(c => c.Key));
        Assert.Equal(Enumerable.Range(0, 9), page.Columns.Select(c => c.Index));
    }

    [Fact]
    public async Task Columns_TakeTitlesFromDisplayAndNestedAttributes()
    {
        var page = await TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(1))).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal("Order #", page.Columns.Single(c => c.Key == "Id").Title);
        Assert.Equal("Name", page.Columns.Single(c => c.Key == "Name").Title);
        Assert.Equal("Vendor code", page.Columns.Single(c => c.Key == "Vendor.Code").Title);
    }

    [Fact]
    public async Task Columns_CarryTypeBehaviorAndDisplayMetadata()
    {
        var page = await TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(1))).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        var name = page.Columns.Single(c => c.Key == "Name");
        var approved = page.Columns.Single(c => c.Key == "ApprovedOrDefault");
        var status = page.Columns.Single(c => c.Key == "Status");

        Assert.Equal(MaviGridPropertyType.String, name.PropertyType);
        Assert.Equal(MaviGridColumnCellBehavior.Navigational, name.CellBehavior);
        Assert.Equal(("Yes", "No"), (approved.TrueText, approved.FalseText));
        Assert.True(status.GraphicDisplay);
        Assert.Equal([new MaviGridEnumOption("Open", "Open status"), new MaviGridEnumOption("Closed", "Closed")], status.EnumOptions);
    }

    [Fact]
    public async Task ComputedColumnWithoutDataItemPath_IsNeitherSortableNorFilterable()
    {
        var page = await TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(1))).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        var label = page.Columns.Single(c => c.Key == "DisplayLabel");
        var name = page.Columns.Single(c => c.Key == "Name");

        Assert.False(label.Sortable);
        Assert.False(label.Filterable);
        Assert.True(name.Sortable);
        Assert.True(name.Filterable);
    }

    [Fact]
    public async Task MappedComputedColumn_IsSortable_AndSortsByTheMappedPath()
    {
        var source = new FakeGridDataSource(
        [
            new TestOrder { Id = 1, Vendor = new TestVendor { Code = "B" } },
            new TestOrder { Id = 2, Vendor = new TestVendor { Code = "A" } }
        ]);
        var options = new GridFetchOptions<TestOrder, TestOrderGridModel>
        {
            PropertyPathMap = new GridPropertyPathMap<TestOrder, TestOrderGridModel>().Map(m => m.VendorCode, "Vendor.Code")
        };

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery { SortColumnKey = "VendorCode" }, options, Ct);

        Assert.True(page.Columns.Single(c => c.Key == "VendorCode").Sortable);
        Assert.Equal("Vendor.Code", source.LastGetQuery!.Sort!.Path);
        Assert.Equal([2, 1], page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task NestedColumns_ArePresentAndEmpty_WhenTheNavigationIsNull()
    {
        var page = await TestFactory.Create(new FakeGridDataSource([new TestOrder { Id = 1, Vendor = null }]))
            .BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        var row = Assert.Single(page.Rows);
        Assert.Equal(string.Empty, Text(page, row, "Vendor.Code"));
        Assert.Equal(string.Empty, Text(page, row, "Vendor.IsPreferred"));
    }

    [Fact]
    public async Task Cells_AreFormattedForTheirColumnType()
    {
        var order = new TestOrder
        {
            Id = 1,
            Name = "A",
            Approved = true,
            Status = TestOrderStatus.Open,
            CreatedOn = new DateTime(2026, 1, 2, 10, 30, 0),
            Vendor = new TestVendor { Code = "V1", IsPreferred = true }
        };

        var page = await TestFactory.Create(new FakeGridDataSource([order])).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);
        var row = Assert.Single(page.Rows);

        Assert.Equal("1", Text(page, row, "Id"));
        Assert.Equal("Yes", Text(page, row, "ApprovedOrDefault"));
        Assert.Equal("Open status", Text(page, row, "Status"));
        Assert.Equal("2026-01-02", Text(page, row, "CreatedOn"));
        Assert.Equal("1-A", Text(page, row, "DisplayLabel"));
        Assert.Equal("V1", Text(page, row, "VendorCode"));
        Assert.Equal("V1", Text(page, row, "Vendor.Code"));
        Assert.Equal("True", Text(page, row, "Vendor.IsPreferred"));
    }

    [Fact]
    public async Task CellThemes_ApplyOnlyToGraphicColumns()
    {
        var source = new FakeGridDataSource([new TestOrder { Id = 1, Name = "A", Status = TestOrderStatus.Closed }]);
        var options = new GridFetchOptions<TestOrder, TestOrderGridModel>
        {
            CellThemes = new Dictionary<string, Func<object?, ThemeColorScheme>>
            {
                ["Status"] = value => value is TestOrderStatus.Closed ? ThemeColorScheme.Alert : ThemeColorScheme.Success,
                ["Name"] = _ => ThemeColorScheme.Warning
            }
        };

        var page = await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery(), options, Ct);
        var row = Assert.Single(page.Rows);

        Assert.Equal(ThemeColorScheme.Alert, row.Cells[page.Columns.Single(c => c.Key == "Status").Index].ThemeColorScheme);
        Assert.Equal(default, row.Cells[page.Columns.Single(c => c.Key == "Name").Index].ThemeColorScheme);
    }

    [Fact]
    public async Task Rows_CarryTheModelKeyAndContextActions()
    {
        var page = await TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(2))).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal([1, 2], page.Rows.Select(r => r.Key));
        Assert.Equal("Open", Assert.Single(page.Rows[1].ContextActions).Title);
        Assert.Equal(2, page.Rows[1].ContextActions[0].Key);
    }

    [Fact]
    public async Task Columns_AreBuiltOnce_AndReusedAcrossFetches()
    {
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>().Map(m => m.VendorCode, "Vendor.Code");
        var options = new GridFetchOptions<TestOrder, TestOrderGridModel> { PropertyPathMap = map };
        var factory = TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(3)));

        var first = await factory.BuildPageAsync(new MaviGridQuery(), options, Ct);
        var second = await factory.BuildPageAsync(new MaviGridQuery { PageNumber = 2 }, options, Ct);
        var unmapped = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Same(first.Columns, second.Columns);
        Assert.NotSame(first.Columns, unmapped.Columns);
    }

    private sealed class NoActionsModel : Interfaces.IGridCollectionModel<TestOrder, int>
    {
        [Attributes.Grid.GridDataProperty(1)]
        public int Id { get; set; }

        public int Key => Id;

        public void ConstructFrom(TestOrder dataItem)
        {
        }

        // A model written without nullable checks can return null here.
        public IEnumerable<Interfaces.Grid.IMaviRowContextMenuItem<int>> GetContextActions() => null!;
    }

    [Fact]
    public async Task ModelReturningNoActions_GetsAnEmptyActionList()
    {
        var factory = new Core.Services.GridDataFactory<TestOrder, NoActionsModel, int>(
            new FakeGridDataSource(TestFactory.Orders(1)), [], [], [],
            Microsoft.Extensions.Options.Options.Create(new Core.Options.GridDataFactoryOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Core.Services.GridDataFactory<TestOrder, NoActionsModel, int>>.Instance);

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Empty(Assert.Single(page.Rows).ContextActions);
    }
}

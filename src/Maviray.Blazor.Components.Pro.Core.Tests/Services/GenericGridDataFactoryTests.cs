using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GenericGridDataFactoryTests
{
    private static GenericGridDataFactory<TestOrder, TestOrderGridModel, int> CreateFactory(IReadOnlyList<TestOrder> items) =>
        new(
            NullLogger<GenericGridDataFactory<TestOrder, TestOrderGridModel, int>>.Instance,
            new FakeGridDataSource(items),
            []);

    private static List<TestOrder> SampleOrders() =>
    [
        new() { Id = 1, Name = "Order 1", Approved = true, Vendor = new TestVendor { Code = "B" } },
        new() { Id = 2, Name = "Order 2", Approved = null, Vendor = new TestVendor { Code = "A" } },
        new() { Id = 3, Name = "Order 3", Approved = false, Vendor = new TestVendor { Code = "C" } }
    ];

    [Fact]
    public async Task BuildDataCollection_NoMapSupplied_SortsByIdenticallyNamedProperty_Unchanged()
    {
        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters { OrderByColumnKey = "Name", SortOrder = SortOrder.Ascending };

        var result = await factory.BuildDataCollection(parameters);

        Assert.NotNull(result);
        Assert.Equal(
            ["Order 1", "Order 2", "Order 3"],
            result!.Rows.Select(r => r.Cells.First(c => c.ColumnKey == "Name").Value));
    }

    [Fact]
    public async Task BuildDataCollection_UnmappedCustomProperty_ColumnIsNotSortableOrFilterable()
    {
        var factory = CreateFactory(SampleOrders());

        var result = await factory.BuildDataCollection(new MaviGridDataQueryParameters());

        var displayLabelColumn = result!.Columns.Single(c => c.Key == "DisplayLabel");

        Assert.False(displayLabelColumn.Sortable);
        Assert.False(displayLabelColumn.Filterable);
    }

    [Fact]
    public async Task BuildDataCollection_UnmappedCustomProperty_RequestingSortDoesNotThrow_FallsBackToId()
    {
        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters { OrderByColumnKey = "DisplayLabel", SortOrder = SortOrder.Ascending };

        var result = await factory.BuildDataCollection(parameters);

        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result!.Rows.Select(r => r.Key!));
    }

    [Fact]
    public async Task BuildDataCollection_MappedNavigationProperty_IsSortableAndSortsByThePath()
    {
        var propertyPathMap = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.VendorCode, "Vendor.Code");

        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters { OrderByColumnKey = "VendorCode", SortOrder = SortOrder.Ascending };

        var result = await factory.BuildDataCollection(parameters, propertyPathMap: propertyPathMap);

        var vendorCodeColumn = result!.Columns.Single(c => c.Key == "VendorCode");
        Assert.True(vendorCodeColumn.Sortable);
        Assert.True(vendorCodeColumn.Filterable);

        // Vendor codes are A (Order 2), B (Order 1), C (Order 3).
        Assert.Equal([2, 1, 3], result.Rows.Select(r => r.Key!));
    }

    [Fact]
    public async Task BuildDataCollection_MappedPropertyWithNullDefault_FilterMatchesCoalescedNullRow()
    {
        var propertyPathMap = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.ApprovedOrDefault, "Approved", nullDefault: false);

        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters { SortOrder = SortOrder.Ascending };
        parameters.AddFilter("ApprovedOrDefault", "false", MaviGridFilterCriteria.IsFalse);

        var result = await factory.BuildDataCollection(parameters, propertyPathMap: propertyPathMap);

        // Order 2 (Approved: null) and Order 3 (Approved: false) both coalesce to false.
        Assert.Equal([2, 3], result!.Rows.Select(r => r.Key!));
    }

    [Fact]
    public async Task BuildDataCollection_UnresolvableFilterKey_DropsTermInsteadOfThrowing()
    {
        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters();
        parameters.AddFilter("DisplayLabel", "anything", MaviGridFilterCriteria.Contains);

        var result = await factory.BuildDataCollection(parameters);

        Assert.NotNull(result);
        Assert.Equal(3, result!.TotalRows);
    }
}

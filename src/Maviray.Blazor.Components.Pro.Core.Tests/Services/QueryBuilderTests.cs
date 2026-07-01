using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class QueryBuilderTests
{
    [Fact]
    public void BuildFilterExpression_NoNullDefault_ExcludesNullRowsFromIsFalse()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Approved = false },
            new() { Id = 2, Approved = null },
            new() { Id = 3, Approved = true }
        }.AsQueryable();

        var filter = QueryBuilder<TestOrder, int>.BuildFilterExpression("Approved", MaviGridFilterCriteria.IsFalse, false);

        var matched = items.Where(filter).Select(x => x.Id).ToList();

        Assert.Equal([1], matched);
    }

    [Fact]
    public void BuildFilterExpression_WithNullDefaultFalse_IsFalseMatchesNullAndFalse()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Approved = false },
            new() { Id = 2, Approved = null },
            new() { Id = 3, Approved = true }
        }.AsQueryable();

        var filter = QueryBuilder<TestOrder, int>.BuildFilterExpression("Approved", MaviGridFilterCriteria.IsFalse, false, nullDefault: false);

        var matched = items.Where(filter).Select(x => x.Id).ToList();

        Assert.Equal([1, 2], matched);
    }

    [Fact]
    public void BuildFilterExpression_NestedPath_FiltersByNavigationProperty()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Vendor = new TestVendor { Code = "A" } },
            new() { Id = 2, Vendor = new TestVendor { Code = "B" } }
        }.AsQueryable();

        var filter = QueryBuilder<TestOrder, int>.BuildFilterExpression("Vendor.Code", MaviGridFilterCriteria.Equals, "B");

        var matched = items.Where(filter).Select(x => x.Id).ToList();

        Assert.Equal([2], matched);
    }

    [Fact]
    public void BuildFilterExpression_NonNullableValueTypeThroughOptionalNavigation_WithNullDefault_DoesNotThrow()
    {
        // Only non-null Vendor rows here - proving a null intermediate navigation resolves safely
        // requires a real EF Core provider translating the LEFT JOIN, which this in-memory
        // LINQ-to-Objects suite cannot exercise. That end-to-end case is proven separately, against
        // a real database, by the "Custom Property Mapping" sample page (see the property-mapping
        // sample plan, Task 7).
        var items = new List<TestOrder>
        {
            new() { Id = 1, Vendor = new TestVendor { IsPreferred = true } },
            new() { Id = 2, Vendor = new TestVendor { IsPreferred = false } }
        }.AsQueryable();

        var filter = QueryBuilder<TestOrder, int>.BuildFilterExpression("Vendor.IsPreferred", MaviGridFilterCriteria.Equals, true, nullDefault: false);

        var matched = items.Where(filter).Select(x => x.Id).ToList();

        Assert.Equal([1], matched);
    }
}

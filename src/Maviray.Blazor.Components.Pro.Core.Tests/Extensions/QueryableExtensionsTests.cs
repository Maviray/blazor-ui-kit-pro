using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Extensions;

public class QueryableExtensionsTests
{
    [Fact]
    public void OrderByDynamic_WithNullDefaultFalse_TreatsNullAsFalse()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Approved = true },
            new() { Id = 2, Approved = null },
            new() { Id = 3, Approved = false }
        }.AsQueryable();

        var ordered = items.OrderByDynamic("Approved", SortOrder.Ascending, nullDefault: false).Select(x => x.Id).ToList();

        // Both the null row (Id 2) and the explicit false row (Id 3) coalesce to false and sort
        // before the true row (Id 1, always last); their relative order is whichever stable sort picks.
        Assert.Equal(1, ordered[2]);
        Assert.Contains(2, ordered.Take(2));
        Assert.Contains(3, ordered.Take(2));
    }

    [Fact]
    public void OrderByDynamic_NoNullDefault_MatchesTodaysBehavior()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Name = "B" },
            new() { Id = 2, Name = "A" }
        }.AsQueryable();

        var ordered = items.OrderByDynamic("Name", SortOrder.Ascending).Select(x => x.Id).ToList();

        Assert.Equal([2, 1], ordered);
    }

    [Fact]
    public void OrderByDynamic_NestedPath_SortsByNavigationProperty()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Vendor = new TestVendor { Code = "B" } },
            new() { Id = 2, Vendor = new TestVendor { Code = "A" } }
        }.AsQueryable();

        var ordered = items.OrderByDynamic("Vendor.Code", SortOrder.Ascending).Select(x => x.Id).ToList();

        Assert.Equal([2, 1], ordered);
    }
}

using Maviray.Blazor.Components.Core.Enums;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public class FakeGridDataSourceTests
{
    [Fact]
    public async Task Get_ThreadsOrderByNullDefault_IntoCoalescedOrdering()
    {
        var source = new FakeGridDataSource(
        [
            new TestOrder { Id = 1, Approved = true },
            new TestOrder { Id = 2, Approved = null },
            new TestOrder { Id = 3, Approved = false }
        ]);

        var result = await source.Get(SortOrder.Ascending, "Approved", 0, 10, orderByNullDefault: false);

        var ids = result!.Select(x => x.Id).ToList();

        Assert.Equal(1, ids[2]);
    }

    [Fact]
    public async Task Count_WithFilter_ReturnsMatchingCount()
    {
        var source = new FakeGridDataSource(
        [
            new TestOrder { Id = 1, Name = "A" },
            new TestOrder { Id = 2, Name = "B" }
        ]);

        var count = await source.Count(x => x.Name == "A");

        Assert.Equal(1, count);
    }
}

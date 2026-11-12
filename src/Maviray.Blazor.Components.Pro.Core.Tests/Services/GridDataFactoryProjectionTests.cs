using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

/// <summary>Loading only the members a grid shows (plus the ones its model reads), instead of whole entities.</summary>
public class GridDataFactoryProjectionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static List<TestOrder> Orders() =>
    [
        new() { Id = 1, Name = "Alpha", Approved = true, Secret = "s", Vendor = new TestVendor { Code = "V1", IsPreferred = true } },
        new() { Id = 2, Name = "Beta", Approved = false, Secret = "s", Vendor = null }
    ];

    private static GridFetchOptions<TestOrder, TestOrderGridModel> Projected(GridProjection<TestOrder> projection) => new() { Projection = projection };

    [Fact]
    public async Task WithoutAProjection_WholeItemsAndTheirNavigationsAreLoaded()
    {
        var source = new FakeGridDataSource(Orders());

        await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Null(source.LastGetQuery!.SelectPaths);
        Assert.Equal(["Vendor"], source.LastGetQuery.IncludePaths);
    }

    [Fact]
    public async Task WithAProjection_TheColumnPathsAndTheListedPathsAreLoaded_WithoutIncludes()
    {
        var source = new FakeGridDataSource(Orders());

        await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery(), Projected(new GridProjection<TestOrder>().Load(o => o.Approved)), Ct);

        // Columns with a data item path: Id, Name, Status, CreatedOn, Vendor.IsPreferred, Vendor.Code. The model also reads Approved.
        Assert.Equal(["Approved", "CreatedOn", "Id", "Name", "Status", "Vendor.Code", "Vendor.IsPreferred"], source.LastGetQuery!.SelectPaths!.Order(StringComparer.Ordinal));
        Assert.Empty(source.LastGetQuery.IncludePaths);
    }

    [Fact]
    public async Task WithAProjection_TheModelSeesTheLoadedMembers()
    {
        var page = await TestFactory.Create(new FakeGridDataSource(Orders()))
            .BuildPageAsync(new MaviGridQuery(), Projected(new GridProjection<TestOrder>().Load(o => o.Approved)), Ct);

        var cells = page.Columns.Select(c => c.Key).Zip(page.Rows[0].Cells, (key, cell) => (key, cell.Text)).ToDictionary();
        Assert.Equal("1-Alpha", cells["DisplayLabel"]);
        Assert.Equal("Yes", cells["ApprovedOrDefault"]);
        Assert.Equal("V1", cells["VendorCode"]);
        Assert.Equal("", page.Rows[1].Cells[page.Columns.Single(c => c.Key == "Vendor.Code").Index].Text);
    }

    [Fact]
    public async Task WithAProjection_AMemberTheModelReadsButNobodyListed_IsNotLoaded()
    {
        // ApprovedOrDefault comes from Approved in ConstructFrom, which isn't a column path: unlisted, it reads false.
        var page = await TestFactory.Create(new FakeGridDataSource(Orders())).BuildPageAsync(new MaviGridQuery(), Projected(new GridProjection<TestOrder>()), Ct);

        Assert.Equal("No", page.Rows[0].Cells[page.Columns.Single(c => c.Key == "ApprovedOrDefault").Index].Text);
    }

    [Fact]
    public async Task WithAProjection_HiddenColumnsAreNotLoaded()
    {
        var source = new FakeGridDataSource(Orders());
        var hideName = new StubAccessPolicy(() => GridAccessDecision<TestOrder>.Allow().WithHiddenColumns("Name"));

        await TestFactory.Create(source, policies: [hideName]).BuildPageAsync(new MaviGridQuery(), Projected(new GridProjection<TestOrder>()), Ct);

        Assert.DoesNotContain("Name", source.LastGetQuery!.SelectPaths!);
    }
}

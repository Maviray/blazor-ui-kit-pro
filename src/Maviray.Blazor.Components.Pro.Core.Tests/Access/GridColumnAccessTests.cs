using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Access;

/// <summary>Columns a policy hides: gone from the page, and unusable for sorting and filtering.</summary>
public class GridColumnAccessTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static List<TestOrder> Orders() =>
    [
        new() { Id = 1, Name = "Alpha", CreatedOn = new DateTime(2026, 1, 2), Vendor = new TestVendor { Code = "V1" } },
        new() { Id = 2, Name = "Beta", CreatedOn = new DateTime(2026, 3, 4), Vendor = new TestVendor { Code = "V2" } }
    ];

    private static StubAccessPolicy Hiding(params string[] columns) => new(() => GridAccessDecision<TestOrder>.Allow().WithHiddenColumns(columns));

    [Fact]
    public async Task HiddenColumns_AreLeftOutOfTheColumnsAndTheCells()
    {
        var factory = TestFactory.Create(new FakeGridDataSource(Orders()), policies: [Hiding("Name", "Vendor.Code")]);

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(["Id", "VendorCode", "DisplayLabel", "ApprovedOrDefault", "Status", "CreatedOn", "Vendor.IsPreferred"], page.Columns.Select(c => c.Key));
        Assert.Equal(Enumerable.Range(0, 7), page.Columns.Select(c => c.Index));
        Assert.All(page.Rows, r => Assert.Equal(7, r.Cells.Count));
        Assert.Equal("2026-01-02", page.Rows[0].Cells[page.Columns.Single(c => c.Key == "CreatedOn").Index].Text);
        Assert.DoesNotContain(page.Rows.SelectMany(r => r.Cells), c => c.Text is "Alpha" or "Beta");
    }

    [Fact]
    public async Task SortAndFilterOnAHiddenColumn_AreIgnored()
    {
        // Filtering on a hidden column would let a user find its values one guess at a time.
        var source = new FakeGridDataSource(Orders());
        var factory = TestFactory.Create(source, policies: [Hiding("Name")]);
        var query = new MaviGridQuery { SortColumnKey = "Name", Filters = [new MaviGridColumnFilter("Name", MaviGridFilterCriteria.Contains, "Alp")] };

        var page = await factory.BuildPageAsync(query, cancellationToken: Ct);

        Assert.Equal(2, page.TotalRows);
        Assert.Null(source.LastGetQuery!.Sort);
        Assert.Null(page.SortColumnKey);
    }

    [Fact]
    public async Task HiddenColumnsOfEveryPolicy_AreCombined()
    {
        var notApplicableHiding = new StubAccessPolicy(() => GridAccessDecision<TestOrder>.NotApplicable().WithHiddenColumns("Status"));
        var factory = TestFactory.Create(new FakeGridDataSource(Orders()), policies: [Hiding("Name"), notApplicableHiding]);

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.DoesNotContain(page.Columns, c => c.Key is "Name" or "Status");
    }

    [Fact]
    public async Task HidingColumns_DoesNotAllowRows()
    {
        var hideOnly = new StubAccessPolicy(() => GridAccessDecision<TestOrder>.NotApplicable().WithHiddenColumns("Name"));
        var factory = TestFactory.Create(new FakeGridDataSource(Orders()), policies: [hideOnly], options: new GridDataFactoryOptions { RequireExplicitAllow = true });

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.True(page.AccessDenied);
    }

    [Fact]
    public async Task TheSameHiddenColumns_GiveTheSameColumnList_SoTheGridKeepsItsLayout()
    {
        var factory = TestFactory.Create(new FakeGridDataSource(Orders()), policies: [Hiding("Name")]);

        var first = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);
        var second = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Same(first.Columns, second.Columns);
    }

    [Fact]
    public async Task UnknownHiddenColumn_IsIgnored()
    {
        var factory = TestFactory.Create(new FakeGridDataSource(Orders()), policies: [Hiding("NoSuchColumn")]);

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(9, page.Columns.Count);
    }

    private sealed class HideNameOfOrders : GridAccessPolicy<TestOrder>
    {
        protected override ValueTask<GridAccessDecision<TestOrder>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GridAccessDecision<TestOrder>.Allow(x => x.Id > 0).WithHiddenColumns("Name"));
    }

    [Fact]
    public async Task TypedPolicyForABaseType_HidesTheColumnsOfASubtypesGrid()
    {
        IGridAccessPolicy policy = new HideNameOfOrders();

        var decision = await policy.EvaluateAsync<TestRushOrder>(new GridAccessRequest(typeof(TestRushOrder), typeof(TestOrderGridModel), null), Ct);

        Assert.Equal(["Name"], decision.HiddenColumns);
        Assert.NotNull(decision.RowFilter);
    }

    [Fact]
    public void ADeniedDecision_CannotHideColumns()
    {
        Assert.Throws<InvalidOperationException>(() => GridAccessDecision<TestOrder>.Deny().WithHiddenColumns("Name"));
    }

    [Fact]
    public void PageBuilder_LeavesOutTheHiddenColumns()
    {
        var page = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Build(
            new GridLoadedPage<TestOrderDto>([new TestOrderDto(1, "Alpha", new DateTime(2026, 1, 2))], 1, 10, 1) { HiddenColumns = ["Name"] },
            new MaviGridQuery { SortColumnKey = "Name" });

        Assert.Equal(["CreatedOn", "Total"], page.Columns.Select(c => c.Key));
        Assert.Equal(["2026-01-02", "#1"], page.Rows[0].Cells.Select(c => c.Text));
        Assert.Null(page.SortColumnKey);
        Assert.Equal(["CreatedOn", "Total"], GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Columns(hiddenColumns: ["Name"]).Select(c => c.Key));
    }
}

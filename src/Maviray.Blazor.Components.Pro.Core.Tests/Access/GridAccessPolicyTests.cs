using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Access;

public class GridAccessPolicyTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static FakeGridDataSource ThreeOrders() => new(TestFactory.Orders(3));

    [Fact]
    public async Task Deny_SkipsDataSourceAndGlobalFilters_AndReturnsAColumnOnlyDeniedPage()
    {
        var source = ThreeOrders();
        var globals = new RecordingGlobalFilter();
        var factory = TestFactory.Create(source, policies: [StubAccessPolicy.Denying("nope")], globalFilters: [globals]);

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.True(page.AccessDenied);
        Assert.Equal("nope", page.AccessDeniedMessage);
        Assert.Equal(9, page.Columns.Count);
        Assert.All(page.Columns, c => Assert.False(c.Sortable || c.Filterable));
        Assert.Empty(page.Rows);
        Assert.Equal(0, page.TotalRows);
        Assert.Equal(0, page.TotalPages);
        Assert.Equal(0, source.CountCalls);
        Assert.Equal(0, source.GetCalls);
        Assert.Equal(0, globals.Calls);
    }

    [Fact]
    public async Task NoPolicies_DefaultOptions_FetchRuns()
    {
        var page = await TestFactory.Create(ThreeOrders()).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.False(page.AccessDenied);
        Assert.Equal(3, page.TotalRows);
    }

    [Fact]
    public async Task AllNotApplicable_DefaultOptions_FetchRuns()
    {
        var factory = TestFactory.Create(ThreeOrders(), policies: [StubAccessPolicy.NotApplicable(), StubAccessPolicy.NotApplicable()]);

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(3, page.TotalRows);
    }

    [Fact]
    public async Task AllNotApplicable_RequireExplicitAllow_Denies()
    {
        var source = ThreeOrders();
        var factory = TestFactory.Create(source, policies: [StubAccessPolicy.NotApplicable()], options: new GridDataFactoryOptions { RequireExplicitAllow = true });

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.True(page.AccessDenied);
        Assert.Null(page.AccessDeniedMessage);
        Assert.Equal(0, source.CountCalls);
    }

    [Fact]
    public async Task NoPolicies_RequireExplicitAllow_Denies()
    {
        var factory = TestFactory.Create(ThreeOrders(), options: new GridDataFactoryOptions { RequireExplicitAllow = true });

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.True(page.AccessDenied);
    }

    [Fact]
    public async Task AllowRowFilter_AppliesToTheCountAndThePage()
    {
        var factory = TestFactory.Create(ThreeOrders(), policies: [StubAccessPolicy.Allowing(x => x.Id != 2)]);

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(2, page.TotalRows);
        Assert.Equal([1, 3], page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task TwoAllowFilters_AreAndCombined()
    {
        var factory = TestFactory.Create(ThreeOrders(), policies: [StubAccessPolicy.Allowing(x => x.Id != 2), StubAccessPolicy.Allowing(x => x.Id != 3)]);

        var page = await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(1, page.TotalRows);
        Assert.Equal([1], page.Rows.Select(r => r.Key));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AllowAndDeny_InEitherOrder_Denies(bool denyFirst)
    {
        var source = ThreeOrders();
        IGridAccessPolicy[] policies = denyFirst
            ? [StubAccessPolicy.Denying(), StubAccessPolicy.Allowing()]
            : [StubAccessPolicy.Allowing(), StubAccessPolicy.Denying()];

        var page = await TestFactory.Create(source, policies: policies).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.True(page.AccessDenied);
        Assert.Equal(0, source.GetCalls);
    }

    [Fact]
    public async Task FirstDeny_StopsEvaluation()
    {
        var probe = StubAccessPolicy.Allowing();

        await TestFactory.Create(ThreeOrders(), policies: [StubAccessPolicy.Denying(), probe]).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Empty(probe.Requests);
    }

    [Fact]
    public async Task ThrowingPolicy_Propagates_AndSkipsTheDataSource()
    {
        var source = ThreeOrders();
        var throwing = new StubAccessPolicy(() => throw new InvalidOperationException("policy failed"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TestFactory.Create(source, policies: [throwing]).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct));

        Assert.Equal("policy failed", ex.Message);
        Assert.Equal(0, source.CountCalls);
    }

    [Fact]
    public async Task PolicyReturningNoDecision_Throws_AndSkipsTheDataSource()
    {
        var source = ThreeOrders();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TestFactory.Create(source, policies: [new StubAccessPolicy(() => null)]).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct));

        Assert.Equal(0, source.CountCalls);
    }

    [Fact]
    public async Task Request_CarriesTypesAccessTagAndToken()
    {
        var policy = StubAccessPolicy.Allowing();
        using var cts = new CancellationTokenSource();

        await TestFactory.Create(ThreeOrders(), policies: [policy]).BuildPageAsync(
            new MaviGridQuery(),
            new GridFetchOptions<TestOrder, TestOrderGridModel> { AccessTag = "admin-list" },
            cts.Token);

        Assert.Equal(new GridAccessRequest(typeof(TestOrder), typeof(TestOrderGridModel), "admin-list"), Assert.Single(policy.Requests));
        Assert.Equal(cts.Token, policy.Tokens[0]);
    }

    [Fact]
    public async Task AlreadyCancelledToken_DoesNotEvaluatePolicies()
    {
        var policy = StubAccessPolicy.Allowing();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TestFactory.Create(ThreeOrders(), policies: [policy]).BuildPageAsync(new MaviGridQuery(), cancellationToken: new CancellationToken(canceled: true)));

        Assert.Empty(policy.Requests);
    }

    [Fact]
    public void SharedDecisions_AreReused()
    {
        Assert.Same(GridAccessDecision<TestOrder>.NotApplicable(), GridAccessDecision<TestOrder>.NotApplicable());
        Assert.Same(GridAccessDecision<TestOrder>.Allow(), GridAccessDecision<TestOrder>.Allow());
        Assert.Equal(GridAccessOutcome.Denied, GridAccessDecision<TestOrder>.Deny("m").Outcome);
    }

    private sealed class OrdersAfterFirstPolicy : GridAccessPolicy<TestOrder>
    {
        protected override ValueTask<GridAccessDecision<TestOrder>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GridAccessDecision<TestOrder>.Allow(x => x.Id > 1));
    }

    [Fact]
    public async Task TypedPolicy_ForItsEntity_AppliesItsTypedDecision()
    {
        var page = await TestFactory.Create(ThreeOrders(), policies: [new OrdersAfterFirstPolicy()]).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal([2, 3], page.Rows.Select(r => r.Key));
    }

    [Fact]
    public async Task TypedPolicy_ForAnotherEntity_IsNotApplicable()
    {
        IGridAccessPolicy policy = new OrdersAfterFirstPolicy();

        var decision = await policy.EvaluateAsync<TestVendor>(new GridAccessRequest(typeof(TestVendor), typeof(TestVendorModel), null), Ct);

        Assert.Equal(GridAccessOutcome.NotApplicable, decision.Outcome);
    }

    private static GridAccessRequest RushOrderRequest => new(typeof(TestRushOrder), typeof(TestOrderGridModel), null);

    [Fact]
    public async Task TypedPolicy_ForABaseType_FiltersADerivedTypesRows()
    {
        IGridAccessPolicy policy = new OrdersAfterFirstPolicy();
        TestRushOrder[] rushOrders = [new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 }];

        var decision = await policy.EvaluateAsync<TestRushOrder>(RushOrderRequest, Ct);

        Assert.Equal(GridAccessOutcome.Allowed, decision.Outcome);
        Assert.Equal([2, 3], rushOrders.AsQueryable().Where(decision.RowFilter!).Select(x => x.Id));
    }

    private sealed class DenyingOrdersPolicy : GridAccessPolicy<TestOrder>
    {
        protected override ValueTask<GridAccessDecision<TestOrder>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GridAccessDecision<TestOrder>.Deny("orders are restricted"));
    }

    [Fact]
    public async Task TypedPolicy_ForABaseType_DeniesADerivedType_WithItsMessage()
    {
        IGridAccessPolicy policy = new DenyingOrdersPolicy();

        var decision = await policy.EvaluateAsync<TestRushOrder>(RushOrderRequest, Ct);

        Assert.Equal(GridAccessOutcome.Denied, decision.Outcome);
        Assert.Equal("orders are restricted", decision.DeniedMessage);
    }

    private sealed class RushOrdersOnlyPolicy : GridAccessPolicy<TestRushOrder>
    {
        protected override ValueTask<GridAccessDecision<TestRushOrder>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GridAccessDecision<TestRushOrder>.Deny());
    }

    [Fact]
    public async Task TypedPolicy_ForADerivedType_IsNotApplicableToItsBase()
    {
        IGridAccessPolicy policy = new RushOrdersOnlyPolicy();

        var decision = await policy.EvaluateAsync<TestOrder>(new GridAccessRequest(typeof(TestOrder), typeof(TestOrderGridModel), null), Ct);

        Assert.Equal(GridAccessOutcome.NotApplicable, decision.Outcome);
    }

    private sealed class OwnerSevenPolicy : GridAccessPolicy<IOwnedItem>
    {
        protected override ValueTask<GridAccessDecision<IOwnedItem>> EvaluateAsync(GridAccessRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GridAccessDecision<IOwnedItem>.Allow(x => x.OwnerId == 7));
    }

    [Fact]
    public async Task TypedPolicy_ForAnInterface_FiltersTheRowsOfTypesThatImplementIt()
    {
        IGridAccessPolicy policy = new OwnerSevenPolicy();
        DerivedOwnedItem[] items = [new() { Id = 1, OwnerId = 3 }, new() { Id = 2, OwnerId = 7 }];

        var decision = await policy.EvaluateAsync<DerivedOwnedItem>(new GridAccessRequest(typeof(DerivedOwnedItem), typeof(object), null), Ct);

        Assert.Equal([2], items.AsQueryable().Where(decision.RowFilter!).Select(x => x.Id));
    }
}

using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Access;

public class GridFetchObserverTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RowsLoaded_ReportsKeysInDisplayOrder_WithPageAndTotals()
    {
        var observer = new RecordingFetchObserver();
        var factory = TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(12)), observers: [observer]);

        await factory.BuildPageAsync(new MaviGridQuery { SortColumnKey = "Id", SortOrder = SortOrder.Descending, PageSize = 10, PageNumber = 2 }, cancellationToken: Ct);

        var loaded = Assert.Single(observer.Loaded);
        Assert.Equal([2, 1], loaded.RowKeys);
        Assert.Equal(2, loaded.PageNumber);
        Assert.Equal(10, loaded.PageSize);
        Assert.Equal(12, loaded.TotalRows);
    }

    [Fact]
    public async Task RowsLoaded_IsReportedForAnEmptyPage()
    {
        var observer = new RecordingFetchObserver();

        await TestFactory.Create(new FakeGridDataSource([]), observers: [observer]).BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        var loaded = Assert.Single(observer.Loaded);
        Assert.Empty(loaded.RowKeys);
        Assert.Equal(0, loaded.TotalRows);
    }

    [Fact]
    public async Task PolicyDenial_IsReportedOnce_WithItsMessage()
    {
        var observer = new RecordingFetchObserver();
        var factory = TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(3)), policies: [StubAccessPolicy.Denying("nope")], observers: [observer]);

        await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal("nope", Assert.Single(observer.Denied).Message);
        Assert.Empty(observer.Loaded);
    }

    [Fact]
    public async Task ExplicitAllowDenial_IsReported_WithNoMessage()
    {
        var observer = new RecordingFetchObserver();
        var factory = TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(3)), observers: [observer], options: new GridDataFactoryOptions { RequireExplicitAllow = true });

        await factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Null(Assert.Single(observer.Denied).Message);
    }

    [Fact]
    public async Task ObserverException_Propagates()
    {
        var observer = new RecordingFetchObserver { ThrowOnLoaded = new InvalidOperationException("audit down") };
        var factory = TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(3)), observers: [observer]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct));

        Assert.Equal("audit down", ex.Message);
    }

    [Fact]
    public async Task Observers_ReceiveTheRequestAccessTagAndToken()
    {
        var observer = new RecordingFetchObserver();
        using var cts = new CancellationTokenSource();

        await TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(3)), observers: [observer]).BuildPageAsync(
            new MaviGridQuery(),
            new GridFetchOptions<TestOrder, TestOrderGridModel> { AccessTag = "audit-tag" },
            cts.Token);

        Assert.Equal("audit-tag", observer.Loaded[0].Request.AccessTag);
        Assert.Equal(cts.Token, observer.Tokens[0]);
    }

    private sealed class NoOpObserver : IGridFetchObserver;

    [Fact]
    public async Task ObserverUsingOnlyTheDefaults_DoesNothing()
    {
        var page = await TestFactory.Create(new FakeGridDataSource(TestFactory.Orders(3)), observers: [new NoOpObserver()])
            .BuildPageAsync(new MaviGridQuery(), cancellationToken: Ct);

        Assert.Equal(3, page.TotalRows);
    }
}

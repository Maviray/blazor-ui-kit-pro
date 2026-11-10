using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GridDataFactoryCancellationTests
{
    [Fact]
    public async Task BuildPageAsync_PassesTheToken_ToCountAndPage()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(1));
        using var cts = new CancellationTokenSource();

        await TestFactory.Create(source).BuildPageAsync(new MaviGridQuery(), cancellationToken: cts.Token);

        Assert.Equal(cts.Token, source.LastCountToken);
        Assert.Equal(cts.Token, source.LastGetToken);
    }

    [Fact]
    public async Task BuildPageAsync_AlreadyCancelled_ThrowsBeforeAnyDataSourceCall()
    {
        var source = new FakeGridDataSource(TestFactory.Orders(1));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TestFactory.Create(source).BuildPageAsync(new MaviGridQuery(), cancellationToken: new CancellationToken(canceled: true)));

        Assert.Equal(0, source.CountCalls);
        Assert.Equal(0, source.GetCalls);
    }
}

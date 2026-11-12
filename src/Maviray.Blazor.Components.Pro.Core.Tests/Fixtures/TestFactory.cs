using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Options;
using Maviray.Blazor.Components.Pro.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

internal static class TestFactory
{
    public static GridDataFactory<TestOrder, TestOrderGridModel, int> Create(
        IGridDataSource<TestOrder> source,
        IEnumerable<IGridAccessPolicy>? policies = null,
        IEnumerable<IGridFetchObserver>? observers = null,
        IEnumerable<IGridGlobalFilterProvider>? globalFilters = null,
        GridDataFactoryOptions? options = null) =>
        new(
            source,
            globalFilters ?? [],
            policies ?? [],
            observers ?? [],
            MsOptions.Create(options ?? new GridDataFactoryOptions()),
            NullLogger<GridDataFactory<TestOrder, TestOrderGridModel, int>>.Instance);

    public static List<TestOrder> Orders(int count) =>
        Enumerable.Range(1, count).Select(i => new TestOrder { Id = i, Name = $"Order {i:00}" }).ToList();
}

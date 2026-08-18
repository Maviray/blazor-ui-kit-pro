using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public class FakeGridDataSource(IReadOnlyList<TestOrder> items) : IGridDataSource<TestOrder>
{
    public Task<int> Count(Expression<Func<TestOrder, bool>>? filter = null)
    {
        IQueryable<TestOrder> query = items.AsQueryable();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        return Task.FromResult(query.Count());
    }

    public Task<IQueryable<TestOrder>?> Get(
        SortOrder sortOrder,
        string orderBy,
        int skip,
        int numberOfItemsToTake,
        Expression<Func<TestOrder, bool>>? filter = null,
        IReadOnlyCollection<string>? includePaths = null,
        GridIncludeConfigurator<TestOrder>? includeConfigurator = null,
        object? orderByNullDefault = null)
    {
        IQueryable<TestOrder> query = items.AsQueryable();

        if (includeConfigurator != null)
        {
            query = includeConfigurator(query);
        }

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var ordered = query.OrderByDynamic(orderBy, sortOrder, orderByNullDefault).Skip(skip).Take(numberOfItemsToTake);

        return Task.FromResult<IQueryable<TestOrder>?>(ordered);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

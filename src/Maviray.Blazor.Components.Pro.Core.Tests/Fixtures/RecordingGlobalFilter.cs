using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

/// <summary>Global filter that constrains <see cref="TestOrder"/> rows with an optional predicate and counts its calls.</summary>
public sealed class RecordingGlobalFilter(Expression<Func<TestOrder, bool>>? filter = null) : IGridGlobalFilterProvider
{
    public int Calls { get; private set; }

    public Expression<Func<TDataItem, bool>>? GetFilter<TDataItem>() where TDataItem : class
    {
        Calls++;
        return typeof(TDataItem) == typeof(TestOrder) ? (Expression<Func<TDataItem, bool>>?)(object?)filter : null;
    }
}

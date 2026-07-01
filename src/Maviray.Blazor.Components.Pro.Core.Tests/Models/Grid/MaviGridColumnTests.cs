using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Models.Grid;

public class MaviGridColumnTests
{
    [Fact]
    public void NewColumn_DefaultsToSortableAndFilterable()
    {
        var column = new MaviGridColumn();

        Assert.True(column.Sortable);
        Assert.True(column.Filterable);
    }
}

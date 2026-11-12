using Bunit;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;
using Maviray.Blazor.Components.Pro.Grid.UI.Components;
using Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests;

public class MaviGridCallbackTests : MaviGridTestContext
{
    private static MaviGridCollection<int> PageWithAction(MaviGridQuery query)
    {
        var page = GridPages.Page(query, 1);
        var row = page.Rows[0];
        return new MaviGridCollection<int>
        {
            Columns = page.Columns,
            Rows = [new MaviGridRow<int> { Key = row.Key, Cells = row.Cells, ContextActions = [new MaviRowContextMenuItem<int> { Key = 1, Title = "Edit", Icon = "lni lni-pencil-1" }] }],
            PageNumber = 1,
            PageSize = query.PageSize,
            TotalRows = 1,
            TotalPages = 1
        };
    }

    [Fact]
    public void CellClick_RaisesOnCellClick_WithTheRowAndColumn()
    {
        CellClickParameters<int>? clicked = null;
        var grid = RenderGrid(new RecordingFetch(1).Invoke, configure: b => b.Add(x => x.OnCellClick, (CellClickParameters<int> p) => clicked = p));

        grid.Find("[data-row-key='1'] [data-column-key='Name']").Click();

        Assert.Equal(new CellClickParameters<int>(1, "Name", MaviGridColumnCellBehavior.Default), clicked);
    }

    [Fact]
    public async Task ExceptionFromTheAppsCellClickHandler_IsNotSwallowed()
    {
        var grid = RenderGrid(new RecordingFetch(1).Invoke,
            configure: b => b.Add(x => x.OnCellClick, (CellClickParameters<int> _) => throw new InvalidOperationException("app bug")));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => grid.Find("[data-row-key='1'] [data-column-key='Name']").ClickAsync(new()));

        Assert.Equal("app bug", ex.Message);
    }

    [Fact]
    public void RowAction_RaisesOnRowContextMenuClick()
    {
        IMaviRowContextMenuItem<int>? clicked = null;
        var grid = RenderGrid((q, _) => Task.FromResult(PageWithAction(q)),
            new MaviGridParameters { TableRowContextMenuDisplayStyle = Maviray.Blazor.Components.Core.Enums.TableRowContextMenuDisplayStyle.Icons },
            b => b.Add(x => x.OnRowContextMenuClick, (IMaviRowContextMenuItem<int> item) => clicked = item));

        grid.Find("[data-row-key='1'] [data-row-actions] button").Click();

        Assert.Equal("Edit", clicked?.Title);
    }
}

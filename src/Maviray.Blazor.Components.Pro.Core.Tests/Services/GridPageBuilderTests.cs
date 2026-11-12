using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

/// <summary>Grid pages built from rows an app loaded itself, through its own service.</summary>
public class GridPageBuilderTests
{
    private static readonly TestOrderDto[] Dtos =
    [
        new(11, "Alpha", new DateTime(2026, 1, 2, 10, 30, 0)),
        new(12, "Beta", new DateTime(2026, 3, 4, 8, 0, 0))
    ];

    private static GridLoadedPage<TestOrderDto> Loaded(int pageNumber = 2, int pageSize = 10, int totalRows = 25) =>
        new(Dtos, pageNumber, pageSize, totalRows);

    [Fact]
    public void Build_MakesOneRowPerItem_FromTheModelsConstructFrom()
    {
        var page = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Build(Loaded(), new MaviGridQuery());

        Assert.Equal([11, 12], page.Rows.Select(r => r.Key));
        Assert.Equal(["Alpha", "2026-01-02", "#11"], page.Rows[0].Cells.Select(c => c.Text));
        Assert.Equal(["Name", "CreatedOn", "Total"], page.Columns.Select(c => c.Key));
    }

    [Fact]
    public void Build_ColumnsWithoutADataItemMember_AreNeitherSortableNorFilterable()
    {
        var page = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Build(Loaded(), new MaviGridQuery());

        Assert.Equal([true, true, false], page.Columns.Select(c => c.Sortable));
        Assert.Equal([true, true, false], page.Columns.Select(c => c.Filterable));
    }

    [Fact]
    public void Build_TakesThePagingTotalsFromTheLoadedPage()
    {
        var page = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Build(Loaded(pageNumber: 3, pageSize: 10, totalRows: 22), new MaviGridQuery());

        Assert.Equal((3, 10, 22, 3), (page.PageNumber, page.PageSize, page.TotalRows, page.TotalPages));
        Assert.Equal(21, page.FirstRowNumber);
        Assert.Equal(22, page.LastRowNumber);
    }

    [Theory]
    [InlineData("Name", "Name")]
    [InlineData("Total", null)]
    [InlineData("Unknown", null)]
    public void Build_ReportsTheSortOnlyForASortableColumn(string requested, string? reported)
    {
        var page = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Build(Loaded(), new MaviGridQuery { SortColumnKey = requested, SortOrder = SortOrder.Descending });

        Assert.Equal(reported, page.SortColumnKey);
        Assert.Equal(SortOrder.Descending, page.SortOrder);
    }

    [Fact]
    public void Build_CarriesTheCursors()
    {
        var loaded = Loaded() with { NextPageCursor = "after-12", PreviousPageCursor = "before-11" };

        var page = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Build(loaded, new MaviGridQuery());

        Assert.Equal(("after-12", "before-11"), (page.NextPageCursor, page.PreviousPageCursor));
    }

    [Fact]
    public void Build_WithAPathMap_MakesTheMappedColumnSortable()
    {
        var options = new GridFetchOptions<TestOrderDto, TestOrderDtoRow>
        {
            PropertyPathMap = new GridPropertyPathMap<TestOrderDto, TestOrderDtoRow>().Map(m => m.Total, nameof(TestOrderDto.Id))
        };

        var page = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Build(Loaded(), new MaviGridQuery { SortColumnKey = "Total" }, options);

        Assert.True(page.Columns.Single(c => c.Key == "Total").Sortable);
        Assert.Equal("Total", page.SortColumnKey);
    }

    [Fact]
    public void Columns_AreTheBuiltPagesColumns()
    {
        var columns = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Columns();

        Assert.Equal(["Name", "CreatedOn"], columns.Where(c => c.Sortable).Select(c => c.Key));
    }

    [Fact]
    public void Denied_HasTheColumns_ButNoRowsAndNothingSortableOrFilterable()
    {
        var page = GridPageBuilder<TestOrderDto, TestOrderDtoRow, int>.Denied("Ask your manager");

        Assert.True(page.AccessDenied);
        Assert.Equal("Ask your manager", page.AccessDeniedMessage);
        Assert.Equal(["Name", "CreatedOn", "Total"], page.Columns.Select(c => c.Key));
        Assert.All(page.Columns, c => Assert.False(c.Sortable || c.Filterable));
        Assert.Empty(page.Rows);
        Assert.Equal(0, page.TotalRows);
    }
}

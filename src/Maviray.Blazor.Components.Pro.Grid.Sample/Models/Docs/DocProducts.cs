using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models.Docs;

/// <summary>What an app's own service returns: a DTO, free of grid attributes.</summary>
public sealed record ProductDto(int Id, string Name, string Category, decimal Price);

/// <summary>The grid's row for a <see cref="ProductDto"/>: the column attributes live here, not on the DTO.</summary>
public sealed class ProductRow : IGridCollectionModel<ProductDto, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2)]
    public string Name { get; set; } = "";

    [GridDataProperty(3)]
    public string Category { get; set; } = "";

    [GridDataProperty(4)]
    public decimal Price { get; set; }

    // Not a DTO member, so it can't be sorted or filtered.
    [Display(Name = "Price band")]
    [GridDataProperty(5)]
    public string PriceBand { get; set; } = "";

    public int Key => Id;

    public void ConstructFrom(ProductDto dataItem) => PriceBand = dataItem.Price < 50 ? "Budget" : "Premium";

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

/// <summary>A page request in the service's own terms.</summary>
public sealed record ProductPageRequest(
    int Page, int PageSize, string SortBy, SortOrder SortOrder, string? NameContains, int? KnownTotal, MaviGridCursor? Cursor);

/// <summary>A page in the service's own terms.</summary>
public sealed record ProductPage(IReadOnlyList<ProductDto> Items, int Page, int PageSize, int TotalCount, string? NextCursor, string? PreviousCursor);

/// <summary>
/// Stands in for an app's own data service (normally EF Core behind your own authorization). It honours the grid's
/// hints: it skips the count when it is told the total, and it seeks from a cursor instead of skipping rows.
/// </summary>
public sealed class ProductService
{
    private static readonly string[] Categories = ["Books", "Garden", "Kitchen", "Toys"];

    private readonly IQueryable<ProductDto> _products = Enumerable.Range(1, 240)
        .Select(i => new ProductDto(i, $"Product {i:000}", Categories[i % Categories.Length], 5 + (i * 37 % 200)))
        .ToArray()
        .AsQueryable();

    public int CountCalls { get; private set; }

    public async Task<ProductPage> GetPageAsync(ProductPageRequest request, CancellationToken cancellationToken)
    {
        var rows = request.NameContains is { Length: > 0 } text
            ? _products.Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            : _products;

        var total = request.KnownTotal ?? Count(rows);
        var page = Math.Clamp(request.Page, 1, Math.Max((total + request.PageSize - 1) / request.PageSize, 1));

        // Sort, then the key to break ties; a cursor from the grid seeks, anything else skips.
        GridOrderKey[] keys = request.SortBy == nameof(ProductDto.Id)
            ? [new GridOrderKey(nameof(ProductDto.Id), request.SortOrder)]
            : [new GridOrderKey(request.SortBy, request.SortOrder), new GridOrderKey(nameof(ProductDto.Id), request.SortOrder)];

        var loaded = await new GridKeyset<ProductDto>(keys).LoadPageAsync(
            rows, page == request.Page ? request.Cursor : null, (page - 1) * request.PageSize, request.PageSize,
            (query, _) => Task.FromResult(query.ToList()), cancellationToken);

        // The total the grid knew is stale when the page holds fewer rows than it predicts: count again.
        if (request.KnownTotal is not null && loaded.Items.Count < Math.Min(request.PageSize, total - (page - 1) * request.PageSize))
        {
            total = Count(rows);
        }

        var lastPage = Math.Max((total + request.PageSize - 1) / request.PageSize, 1);

        return new ProductPage(loaded.Items, page, request.PageSize, total,
            NextCursor: page < lastPage ? loaded.LastRowCursor : null,
            PreviousCursor: page > 1 ? loaded.FirstRowCursor : null);
    }

    private int Count(IQueryable<ProductDto> rows)
    {
        CountCalls++;
        return rows.Count();
    }
}

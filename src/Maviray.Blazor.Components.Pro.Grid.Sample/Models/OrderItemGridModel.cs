using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models;

// Demonstrates the four ways a grid column ends up sortable/filterable (or not):
//   (a) direct 1:1 with OrderItem itself - resolved via identical-name fallback
//   (b) straight passthrough of one related property - resolved via GridPropertyPathMap
//   (c) a coalesced default over a related property - resolved via GridPropertyPathMap with nullDefault
//   (d) genuinely computed from multiple properties - unresolvable, Sortable/Filterable stay false
public class OrderItemGridModel : IGridCollectionModel<OrderItem, int>
{
    // (a) Direct 1:1 with OrderItem - Mapster auto-maps these; ConstructFrom doesn't touch them.
    [Display(Name = "Id")]
    [GridDataProperty(1)]
    public int Id { get; set; }

    [Display(Name = "Product Name")]
    [GridDataProperty(2)]
    public string? ProductName { get; set; }
    
    [GridDataProperty(3)]
    [Display(Name = "Is Backordered")]
    [GridBooleanDisplayValues("Yes", "No")]
    public bool IsBackordered { get; set; }

    [Display(Name = "Quantity")]
    [GridDataProperty(4)]
    public int Quantity { get; set; }

    [Display(Name = "Unit Price")]
    [GridDataProperty(5)]
    public decimal UnitPrice { get; set; }

    // (b) Straight 1:1 passthrough of one related property, no default substitution.
    [Display(Name = "Order Number")]
    [GridDataProperty(6)]
    public string? OrderNumber { get; set; }

    [Display(Name = "Customer Company")]
    [GridDataProperty(7)]
    public string? CustomerCompanyName { get; set; }

    [Display(Name = "Country")]
    [GridDataProperty(8)]
    public string? CountryName { get; set; }

    // (c) Constructed via a coalesce/default over a related property.
    [Display(Name = "Region Rating (or Default)")]
    [GridDataProperty(9)]
    public RatingEnum RegionRatingOrDefault { get; set; }
    
    [GridDataProperty(10)]
    [GridBooleanDisplayValues("Yes", "No")]
    [Display(Name = "Is International (or Default)")]
    public bool IsInternationalOrDefault { get; set; }

    // (d) Genuinely computed - no single backing path, so these stay unresolvable.
    [Display(Name = "Total Line Value")]
    [GridDataProperty(11)]
    public decimal TotalLineValue { get; set; }

    [Display(Name = "Full Description")]
    [GridDataProperty(12)]
    public string? FullDescription { get; set; }

    public int Key => Id;

    public void ConstructFrom(OrderItem dataItem)
    {
        OrderNumber = dataItem.Order?.OrderNumber;
        CustomerCompanyName = dataItem.Order?.Customer?.CompanyName;
        CountryName = dataItem.Order?.Customer?.Region?.Country?.CountryName;

        RegionRatingOrDefault = dataItem.Order?.Customer?.Region?.Rating ?? RatingEnum.Low;
        IsInternationalOrDefault = dataItem.Order?.Customer?.Region?.IsInternational ?? false;

        TotalLineValue = dataItem.Quantity * dataItem.UnitPrice;
        FullDescription = $"{dataItem.ProductName} ({dataItem.Order?.OrderNumber})";
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class OrderItem
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Product Name")]
    public string? ProductName { get; set; }

    [Display(Name = "Product Grade")]
    public char ProductGrade { get; set; }

    [Display(Name = "Is Backordered")]
    public bool IsBackordered { get; set; }

    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [Display(Name = "Unit Price")]
    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Shipped Date")]
    public DateTime? ShippedDate { get; set; }

    [Display(Name = "Estimated Duration")]
    public TimeSpan EstimatedDuration { get; set; }

    [Display(Name = "Tracking Reference")]
    public Guid TrackingReference { get; set; }

    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }
}

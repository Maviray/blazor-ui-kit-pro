using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class Order
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Order Number")]
    public string? OrderNumber { get; set; }

    [Display(Name = "Order Class")]
    public char OrderClass { get; set; }

    [Display(Name = "Is Rush")]
    public bool IsRush { get; set; }

    [Display(Name = "Line Count")]
    public int LineCount { get; set; }

    [Display(Name = "Total Amount")]
    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Order Date")]
    public DateTime OrderDate { get; set; }

    [Display(Name = "Processing Window")]
    public TimeSpan ProcessingWindow { get; set; }

    [Display(Name = "Confirmation Code")]
    public Guid ConfirmationCode { get; set; }

    public int CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }
}

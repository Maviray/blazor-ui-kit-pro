using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class Region
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Region Name")]
    public string? RegionName { get; set; }

    [Display(Name = "Region Code")]
    public char RegionCode { get; set; }

    [Display(Name = "Is International")]
    public bool IsInternational { get; set; }

    [Display(Name = "Office Count")]
    public int OfficeCount { get; set; }

    [Display(Name = "Average Deal Size")]
    [Precision(18, 2)]
    public decimal AverageDealSize { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Established Date")]
    public DateTime EstablishedDate { get; set; }

    [Display(Name = "Standard Shipping Window")]
    public TimeSpan StandardShippingWindow { get; set; }

    [Display(Name = "Region Reference")]
    public Guid RegionReference { get; set; }

    public int CountryId { get; set; }
    public virtual Country? Country { get; set; }
}

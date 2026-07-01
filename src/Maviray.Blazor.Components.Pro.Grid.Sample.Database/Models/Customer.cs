using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class Customer
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Display(Name = "Customer Grade")]
    public char CustomerGrade { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Employee Count")]
    public int EmployeeCount { get; set; }

    [Display(Name = "Credit Limit")]
    [Precision(18, 2)]
    public decimal CreditLimit { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Founded Date")]
    public DateTime FoundedDate { get; set; }

    [Display(Name = "Support Response Time")]
    public TimeSpan SupportResponseTime { get; set; }

    [Display(Name = "Account Reference")]
    public Guid AccountReference { get; set; }

    // Deliberately nullable: the one optional hop in the chain, so a customer can have no region.
    public int? RegionId { get; set; }
    public virtual Region? Region { get; set; }
}

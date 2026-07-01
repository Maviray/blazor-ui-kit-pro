using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class Country
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Country Name")]
    public string? CountryName { get; set; }

    [Display(Name = "Country Grade")]
    public char CountryGrade { get; set; }

    [Display(Name = "Is Member State")]
    public bool IsMemberState { get; set; }

    [Display(Name = "Population (Millions)")]
    public int PopulationMillions { get; set; }

    [Display(Name = "GDP (Trillions)")]
    [Precision(18, 2)]
    public decimal GdpTrillion { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Independence Date")]
    public DateTime IndependenceDate { get; set; }

    [Display(Name = "Average Transit Time")]
    public TimeSpan AverageTransitTime { get; set; }

    [Display(Name = "Country Reference")]
    public Guid CountryReference { get; set; }
}

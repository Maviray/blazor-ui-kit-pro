using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class DbSampleItemOne
{
    [Display(Name = "Id")]
    public int Id { get; set; }
    [Display(Name = "Name")]
    public string? Name { get; set; }
    [Display(Name = "Integer")]
    public int Integer { get; set; }

    [Display(Name = "Decimal")]
    [Precision(18, 2)]
    public decimal Decimal { get; set; }
    [Display(Name = "Long")]
    public long Long { get; set; }
    [Display(Name = "Sample Enum")]
    public SampleEnum SampleEnum { get; set; }
    [Display(Name = "Boolean")]
    public bool Boolean { get; set; }
    [Display(Name = "Date")]
    public DateTime? Date { get; set; }

    public virtual DbSampleItemTwo? DbSampleItemTwo { get; set; }
}
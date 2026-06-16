using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class AuthScope
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Name")]
    public string? Name { get; set; }

    public virtual ICollection<DbSampleItemThree> DbSampleItemThree { get; set; } = new List<DbSampleItemThree>();
    public virtual ICollection<DbSampleItemFour> DbSampleItemFour { get; set; } = new List<DbSampleItemFour>();
}
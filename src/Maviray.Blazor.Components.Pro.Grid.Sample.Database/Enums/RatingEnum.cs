using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;

public enum RatingEnum
{
    [Display(Name = "Low")]
    Low,

    [Display(Name = "Medium")]
    Medium,

    [Display(Name = "High")]
    High,

    [Display(Name = "Critical")]
    Critical
}

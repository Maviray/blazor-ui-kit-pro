using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models;

public class AuthAreaModel
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "AuthArea")]
    [GridNestedProperty("Auth Area")]
    public string? Name { get; set; }
}

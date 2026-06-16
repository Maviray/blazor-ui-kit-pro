using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models;

public class ScopeModel
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Scope")]
    [GridNestedProperty("Scope")]
    public string? Name { get; set; }
}

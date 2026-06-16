using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class DbSampleItemThree : IScopedItem
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Name")]
    public string? Name { get; set; }

    public int? ScopeId { get; set; }

    [Display(Name = "Scope")]
    public virtual Scope? Scope { get; set; }

    public int? AuthAreaId { get; set; }

    [Display(Name = "AuthArea")]
    public virtual AuthArea? AuthArea { get; set; }
}
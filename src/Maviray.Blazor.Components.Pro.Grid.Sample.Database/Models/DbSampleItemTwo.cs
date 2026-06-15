using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class DbSampleItemTwo
{
    [Display(Name = "Id")]
    public int Id { get; set; }
    [Display(Name = "Name")]
    public string? Name { get; set; }
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public int DbSampleItemOneId { get; set; }
    public virtual DbSampleItemOne? DbSampleItemOne { get; set; }
}
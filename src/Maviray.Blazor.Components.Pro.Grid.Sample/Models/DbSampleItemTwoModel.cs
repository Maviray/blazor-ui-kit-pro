using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models;

public class DbSampleItemTwoModel : IGridCollectionModel<DbSampleItemTwo, int>
{
    public int Key => Id;

    [Display(Name = "Id")]
    public int Id { get; set; }
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Display(Name = "Description")]
    [GridNestedProperty("Description")]
    public string? Description { get; set; }

    public int DbSampleItemOneId { get; set; }
    public virtual DbSampleItemOne? DbSampleItemOne { get; set; }

    public void ConstructFrom(DbSampleItemTwo dataItem)
    {
       
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions()
    {
        return Enumerable.Empty<IMaviRowContextMenuItem<int>>();
    }
}
using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models;

public class DbSampleItemThreeModel : IGridCollectionModel<DbSampleItemThree, int>
{
    public int Key => Id;

    [Display(Name = "Id")]
    [GridDataProperty(1, MaviGridColumnCellBehavior.Default)]
    public int Id { get; set; }

    [Display(Name = "Name")]
    [GridDataProperty(2, MaviGridColumnCellBehavior.Default)]
    public string? Name { get; set; }

    // Eager-loaded and projected via the navigation-include pipeline. Names match the entity's
    // navigation properties (Scope, AuthArea) so the includes resolve and Mapster maps them.
    [GridNavProperty(3)]
    public ScopeModel? Scope { get; set; }

    [GridNavProperty(4)]
    public AuthAreaModel? AuthArea { get; set; }

    public void ConstructFrom(DbSampleItemThree dataItem)
    {
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions()
    {
        return Enumerable.Empty<IMaviRowContextMenuItem<int>>();
    }
}

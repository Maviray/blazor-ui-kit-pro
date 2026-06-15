using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models;

public class DbSampleItemOneModel : IGridCollectionModel<DbSampleItemOne, int>
{
    public int Key => Id;

    [Display(Name = "Id")]
    [GridDataProperty(1, MaviGridColumnCellBehavior.Default)]
    public int Id { get; set; }

    [Display(Name = "Name")]
    [GridDataProperty(2, MaviGridColumnCellBehavior.Default)]
    public string? Name { get; set; }


    [Display(Name = "Integer")]
    [GridDataProperty(3, MaviGridColumnCellBehavior.Default)]
    public int Integer { get; set; }

    [Precision(18, 2)]
    [Display(Name = "Decimal")]
    [GridDataProperty(4, MaviGridColumnCellBehavior.Default)]
    public decimal Decimal { get; set; }
    
    [Display(Name = "Long")]
    [GridDataProperty(5, MaviGridColumnCellBehavior.Default)]
    public long Long { get; set; }

    [Display(Name = "Sample Enum")]
    [GridDataProperty(6, MaviGridColumnCellBehavior.Default)]
    public SampleEnum SampleEnum { get; set; }

    [Display(Name = "Boolean")]
    [GridBooleanDisplayValues("Positive", "Negative")]
    [GridDataProperty(7, MaviGridColumnCellBehavior.Default)]
    public bool Boolean { get; set; }

    [Display(Name = "Date")]
    [GridDataProperty(8, MaviGridColumnCellBehavior.Default)]
    public DateTime? Date { get; set; }

    [GridNavProperty(9)]
    public DbSampleItemTwoModel? DbSampleItemTwo { get; set; }

    public void ConstructFrom(DbSampleItemOne dataItem)
    {
       
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions()
    {
        var items = new List<IMaviRowContextMenuItem<int>>();

        items.Add(new MaviRowContextMenuItem<int>()
        {
            Key = Id,
            Title = "Edit",
            Icon = "lni lni-brush-1-rotate",
            ThemeColorScheme = ThemeColorScheme.Primary,
            ElementSize = ElementSize.Small
        });

        items.Add(new MaviRowContextMenuItem<int>()
        {
            Key = Id,
            Title = "Details",
            Icon = "lni lni-brush-1-rotate",
            ThemeColorScheme = ThemeColorScheme.Success,
            ElementSize = ElementSize.Small
        });

        items.Add(new MaviRowContextMenuItem<int>()
        {
            Key = Id,
            Title = "Delete",
            Icon = "lni lni-trash-3",
            ThemeColorScheme = ThemeColorScheme.Alert,
            ElementSize = ElementSize.Small
        });

        return items;
    }
}
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models.Docs;

/// <summary>
/// The documentation page's grid model: four columns of <see cref="DbSampleItemOne"/>. Each property has the same name
/// as a member of the entity, so it is filled automatically and can be sorted and filtered.
/// </summary>
public class DocItemModel : IGridCollectionModel<DbSampleItemOne, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)]
    public string? Name { get; set; }

    [Display(Name = "Amount")]
    [GridDataProperty(3)]
    public int Integer { get; set; }

    [Display(Name = "Created")]
    [GridDataProperty(4)]
    [GridDateTimeFormat("yyyy-MM-dd")]
    public DateTime? Date { get; set; }

    public int Key => Id;

    public virtual void ConstructFrom(DbSampleItemOne dataItem)
    {
    }

    public virtual IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

/// <summary>
/// Every kind of column: plain, badge, enum, boolean text, date format, a column computed in ConstructFrom, and a
/// column of the related <see cref="DbSampleItemTwo"/>.
/// </summary>
public sealed class DocFormattingModel : IGridCollectionModel<DbSampleItemOne, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [Display(Name = "Amount")]
    [GridDataProperty(2)]
    [GridAdvancedDisplay]
    public int Integer { get; set; }

    [Display(Name = "Category")]
    [GridDataProperty(3)]
    [GridAdvancedDisplay]
    public SampleEnum SampleEnum { get; set; }

    [Display(Name = "Status")]
    [GridDataProperty(4)]
    [GridBooleanDisplayValues("Active", "Inactive")]
    public bool Boolean { get; set; }

    [Display(Name = "Created")]
    [GridDataProperty(5)]
    [GridDateTimeFormat("dd MMM yyyy")]
    public DateTime? Date { get; set; }

    [Display(Name = "Price")]
    [GridDataProperty(6)]
    public decimal Decimal { get; set; }

    // Computed in ConstructFrom: shown, but it has no entity member, so it can't be sorted or filtered.
    [Display(Name = "Price incl. VAT")]
    [GridDataProperty(7)]
    public decimal PriceWithVat { get; set; }

    // The related entity: same name as the entity's navigation, which is loaded for the page.
    [GridNavProperty(8)]
    public DocDetailsModel? DbSampleItemTwo { get; set; }

    public int Key => Id;

    public void ConstructFrom(DbSampleItemOne dataItem) => PriceWithVat = Math.Round(dataItem.Decimal * 1.2m, 2);

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

/// <summary>
/// The column-width demo: [GridStretch] on the model makes Name and the related item's columns share the free width.
/// </summary>
public sealed class DocStretchModel : IGridCollectionModel<DbSampleItemOne, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)]
    [GridStretch(2)]
    public string? Name { get; set; }

    [Display(Name = "Amount")]
    [GridDataProperty(3)]
    public int Integer { get; set; }

    // On a navigation property, the mark applies to each of its nested columns (here, Details).
    [GridNavProperty(4)]
    [GridStretch]
    public DocDetailsModel? DbSampleItemTwo { get; set; }

    public int Key => Id;

    public void ConstructFrom(DbSampleItemOne dataItem)
    {
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

/// <summary>The related entity's columns: each [GridNestedProperty] becomes a column keyed "DbSampleItemTwo.Property".</summary>
public sealed class DocDetailsModel
{
    [GridNestedProperty("Details")]
    public string? Description { get; set; }
}

/// <summary>The same columns, with Edit and Delete actions on every row.</summary>
public sealed class DocItemWithActionsModel : DocItemModel
{
    public override IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() =>
    [
        new MaviRowContextMenuItem<int>
        {
            Key = Id, Title = "Edit", Icon = "lni lni-pencil-1",
            ThemeColorScheme = ThemeColorScheme.Primary, ElementVariant = ElementVariant.Text, ElementSize = ElementSize.Small
        },
        new MaviRowContextMenuItem<int>
        {
            Key = Id, Title = "Delete", Icon = "lni lni-trash-3",
            ThemeColorScheme = ThemeColorScheme.Alert, ElementVariant = ElementVariant.Text, ElementSize = ElementSize.Small
        }
    ];
}

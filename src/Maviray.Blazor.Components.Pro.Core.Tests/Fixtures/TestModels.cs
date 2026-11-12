using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public enum TestOrderStatus
{
    [Display(Name = "Open status")]
    Open,
    Closed
}

public class TestVendor
{
    public string? Code { get; set; }
    public bool IsPreferred { get; set; }
}

public class TestOrder
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool? Approved { get; set; }
    public TestOrderStatus Status { get; set; }
    public DateTime CreatedOn { get; set; }
    public TestVendor? Vendor { get; set; }

    // Present on the entity but deliberately not a grid column.
    public string? Secret { get; set; }
}

// A DTO from an app's own service, and a row model that keeps the grid attributes off it: private setters, filled
// in ConstructFrom. Total is shown but has no DTO member, so it can't be sorted or filtered.
public sealed record TestOrderDto(int Id, string Name, DateTime CreatedOn);

public sealed class TestOrderDtoRow : IGridCollectionModel<TestOrderDto, int>
{
    public int Id { get; private set; }

    [GridDataProperty(1)]
    public string Name { get; private set; } = "";

    [GridDataProperty(2)]
    [GridDateTimeFormat("yyyy-MM-dd")]
    public DateTime CreatedOn { get; private set; }

    [GridDataProperty(3)]
    public string Total { get; private set; } = "";

    public int Key => Id;

    public void ConstructFrom(TestOrderDto dataItem)
    {
        Id = dataItem.Id;
        Name = dataItem.Name;
        CreatedOn = dataItem.CreatedOn;
        Total = $"#{dataItem.Id}";
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

// A derived entity, as with EF Core inheritance: a policy for TestOrder governs grids over it too.
public class TestRushOrder : TestOrder
{
    public int Priority { get; set; }
}

public class TestVendorModel
{
    [GridNestedProperty("Vendor preferred")]
    public bool IsPreferred { get; set; }

    [GridNestedProperty("Vendor code")]
    public string? Code { get; set; }
}

// Id, Name, Status and CreatedOn map 1:1 from TestOrder; VendorCode is reachable through a navigation path;
// DisplayLabel is computed and has no data item path; ApprovedOrDefault coalesces a nullable flag.
public class TestOrderGridModel : IGridCollectionModel<TestOrder, int>
{
    [Display(Name = "Order #")]
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2, MaviGridColumnCellBehavior.Navigational)]
    public string? Name { get; set; }

    [GridDataProperty(3)]
    public string? VendorCode { get; set; }

    [GridDataProperty(4)]
    public string? DisplayLabel { get; set; }

    [GridDataProperty(5)]
    [GridBooleanDisplayValues("Yes", "No")]
    public bool ApprovedOrDefault { get; set; }

    [GridDataProperty(6)]
    [GridAdvancedDisplay]
    public TestOrderStatus Status { get; set; }

    [GridDataProperty(7)]
    [GridDateTimeFormat("yyyy-MM-dd")]
    public DateTime CreatedOn { get; set; }

    [GridNavProperty(8)]
    public TestVendorModel? Vendor { get; set; }

    public int Key => Id;

    public void ConstructFrom(TestOrder dataItem)
    {
        VendorCode = dataItem.Vendor?.Code;
        DisplayLabel = $"{dataItem.Id}-{dataItem.Name}";
        ApprovedOrDefault = dataItem.Approved ?? false;
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() =>
        [new MaviRowContextMenuItem<int> { Key = Id, Title = "Open" }];
}

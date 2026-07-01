using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

// Mirrors the shape of a real-world custom-constructed grid model (e.g. PurchaseOrderItemGridModel):
// Id/Name are 1:1 with TestOrder, VendorCode is reachable via a TestOrder navigation path,
// DisplayLabel is irreducibly computed, and ApprovedOrDefault mirrors a "?? false" coalesce.
public class TestOrderGridModel : IGridCollectionModel<TestOrder, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2)]
    public string? Name { get; set; }

    [GridDataProperty(3)]
    public string? VendorCode { get; set; }

    [GridDataProperty(4)]
    public string? DisplayLabel { get; set; }

    [GridDataProperty(5)]
    public bool ApprovedOrDefault { get; set; }

    public int Key => Id;

    public void ConstructFrom(TestOrder dataItem)
    {
        Id = dataItem.Id;
        Name = dataItem.Name;
        VendorCode = dataItem.Vendor?.Code;
        DisplayLabel = $"{dataItem.Id}-{dataItem.Name}";
        ApprovedOrDefault = dataItem.Approved ?? false;
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

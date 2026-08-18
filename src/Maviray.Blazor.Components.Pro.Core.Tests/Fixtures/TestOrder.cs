namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public class TestOrder
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool? Approved { get; set; }
    public TestVendor? Vendor { get; set; }
}

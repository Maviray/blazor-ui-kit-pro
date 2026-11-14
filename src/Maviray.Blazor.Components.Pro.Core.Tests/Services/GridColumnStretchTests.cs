using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using System.Text.Json;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

/// <summary>Stretch weights: read from [GridStretch] on the model and carried on every column the grid gets.</summary>
public class GridColumnStretchTests
{
    private static Dictionary<string, int> Weights(IEnumerable<MaviGridColumn> columns) => columns.ToDictionary(c => c.Key, c => c.StretchWeight);

    [Fact]
    public void Weights_ComeFromGridStretch_OnDataNestedAndNavigationProperties()
    {
        var columns = GridPageBuilder<TestOrder, StretchModel, int>.Columns();

        // Vendor.Code's own mark (3) wins over the navigation's (2); Vendor.IsPreferred takes the navigation's.
        Assert.Equal(new Dictionary<string, int> { ["Id"] = 0, ["Name"] = 1, ["Vendor.Code"] = 3, ["Vendor.IsPreferred"] = 2 }, Weights(columns));
    }

    [Fact]
    public void ColumnsLeftAfterHiding_KeepTheirWeights()
    {
        var columns = GridPageBuilder<TestOrder, StretchModel, int>.Columns(hiddenColumns: ["Id"]);

        Assert.Equal(new Dictionary<string, int> { ["Name"] = 1, ["Vendor.Code"] = 3, ["Vendor.IsPreferred"] = 2 }, Weights(columns));
        Assert.Equal([0, 1, 2], columns.Select(c => c.Index));
    }

    [Fact]
    public void DeniedPageColumns_KeepTheirWeights()
    {
        var page = GridPageBuilder<TestOrder, StretchModel, int>.Denied();

        Assert.Equal(1, page.Columns.Single(c => c.Key == "Name").StretchWeight);
        Assert.Equal(3, page.Columns.Single(c => c.Key == "Vendor.Code").StretchWeight);
    }

    [Fact]
    public void WeightBelowOne_Throws_NamingTheModelAndTheProperty()
    {
        var error = Assert.Throws<InvalidOperationException>(() => GridPageBuilder<TestOrder, ZeroWeightModel, int>.Columns());

        Assert.Contains(nameof(ZeroWeightModel), error.Message);
        Assert.Contains("Name", error.Message);
        Assert.Contains("0", error.Message);
    }

    [Fact]
    public void StretchWeight_RoundTripsThroughJson_AndIsZeroWhenAbsent()
    {
        var column = new MaviGridColumn { Key = "Name", Title = "Name", StretchWeight = 2 };

        Assert.Equal(2, JsonSerializer.Deserialize<MaviGridColumn>(JsonSerializer.Serialize(column))!.StretchWeight);
        Assert.Equal(0, JsonSerializer.Deserialize<MaviGridColumn>("""{"Key":"Name","Title":"Name"}""")!.StretchWeight);
    }

    private sealed class StretchVendorModel
    {
        [GridNestedProperty("Code")]
        [GridStretch(3)]
        public string? Code { get; set; }

        [GridNestedProperty("Preferred")]
        public bool IsPreferred { get; set; }
    }

    private sealed class StretchModel : IGridCollectionModel<TestOrder, int>
    {
        [GridDataProperty(1)]
        public int Id { get; set; }

        [GridDataProperty(2)]
        [GridStretch]
        public string? Name { get; set; }

        [GridNavProperty(3)]
        [GridStretch(2)]
        public StretchVendorModel? Vendor { get; set; }

        // Not a column: the mark is ignored.
        [GridStretch(5)]
        public string? Secret { get; set; }

        public int Key => Id;

        public void ConstructFrom(TestOrder dataItem)
        {
        }

        public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
    }

    private sealed class ZeroWeightModel : IGridCollectionModel<TestOrder, int>
    {
        [GridDataProperty(1)]
        public int Id { get; set; }

        [GridDataProperty(2)]
        [GridStretch(0)]
        public string? Name { get; set; }

        public int Key => Id;

        public void ConstructFrom(TestOrder dataItem)
        {
        }

        public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
    }
}

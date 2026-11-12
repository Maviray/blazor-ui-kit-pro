using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Models.Grid;

public class GridPropertyPathMapTests
{
    [Fact]
    public void Map_ThenTryResolve_ReturnsRegisteredPath()
    {
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.VendorCode, "Vendor.Code");

        var resolved = map.TryResolve("VendorCode", out var path);

        Assert.True(resolved);
        Assert.Equal("Vendor.Code", path.DataItemPath);
        Assert.Null(path.NullDefault);
    }

    [Fact]
    public void Map_WithNullDefault_IsCarriedThroughToResolvedPath()
    {
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.ApprovedOrDefault, "Approved", nullDefault: false);

        map.TryResolve("ApprovedOrDefault", out var path);

        Assert.Equal("Approved", path.DataItemPath);
        Assert.Equal(false, path.NullDefault);
    }

    [Fact]
    public void TryResolve_UnregisteredProperty_ReturnsFalse()
    {
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>();

        var resolved = map.TryResolve("Name", out _);

        Assert.False(resolved);
    }

    [Fact]
    public void Map_BoxedValueTypeProperty_ExtractsPropertyNameFromConvertExpression()
    {
        // ApprovedOrDefault is a bool, so the Expression<Func<TModel, object?>> selector wraps the
        // property access in a Convert node - this guards against GetPropertyName only handling
        // reference-typed properties.
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.ApprovedOrDefault, "Approved");

        var resolved = map.TryResolve("ApprovedOrDefault", out var path);

        Assert.True(resolved);
        Assert.Equal("Approved", path.DataItemPath);
    }

    [Fact]
    public void Map_ReturnsANewMap_AndLeavesTheOriginalUnchanged()
    {
        var empty = new GridPropertyPathMap<TestOrder, TestOrderGridModel>();

        var mapped = empty.Map(m => m.VendorCode, "Vendor.Code");

        Assert.NotSame(empty, mapped);
        Assert.False(empty.TryResolve("VendorCode", out _));
        Assert.True(mapped.TryResolve("VendorCode", out _));
    }
}

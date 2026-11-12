using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Extensions;

public class SelectPathsTests
{
    private static readonly TestOrder[] Orders =
    [
        new() { Id = 1, Name = "Alpha", Secret = "s1", Approved = true, Vendor = new TestVendor { Code = "V1", IsPreferred = true } },
        new() { Id = 2, Name = "Beta", Secret = "s2", Vendor = null }
    ];

    [Fact]
    public void LoadsOnlyTheListedMembers()
    {
        var loaded = Orders.AsQueryable().SelectPaths(["Id", "Name"]).ToList();

        Assert.Equal([1, 2], loaded.Select(o => o.Id));
        Assert.Equal(["Alpha", "Beta"], loaded.Select(o => o.Name));
        Assert.All(loaded, o => Assert.Null(o.Secret));
        Assert.All(loaded, o => Assert.Null(o.Approved));
    }

    [Fact]
    public void NavigationPath_LoadsOnlyThatMemberOfTheRelatedItem_AndKeepsAMissingOneNull()
    {
        var loaded = Orders.AsQueryable().SelectPaths(["Id", "Vendor.Code"]).ToList();

        Assert.Equal("V1", loaded[0].Vendor!.Code);
        Assert.False(loaded[0].Vendor!.IsPreferred);
        Assert.Null(loaded[1].Vendor);
    }

    [Fact]
    public void PathToANavigation_LoadsTheWholeRelatedItem()
    {
        var loaded = Orders.AsQueryable().SelectPaths(["Vendor", "Vendor.Code"]).ToList();

        Assert.True(loaded[0].Vendor!.IsPreferred);
    }

    [Fact]
    public void UnknownPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => Orders.AsQueryable().SelectPaths(["Id", "NoSuchMember"]));
    }

    public sealed class ReadOnlyItem
    {
        public int Id { get; set; }
        public string Name => "fixed";
    }

    [Fact]
    public void MemberWithoutASetter_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new[] { new ReadOnlyItem() }.AsQueryable().SelectPaths(["Id", "Name"]));
    }

    [Fact]
    public void Projection_CollectsPathsFromMemberExpressions_AndStaysImmutable()
    {
        var empty = new GridProjection<TestOrder>();

        var projection = empty.Load(o => o.Approved).Load(o => o.Vendor!.IsPreferred).Load("Secret");

        Assert.Equal(["Approved", "Vendor.IsPreferred", "Secret"], projection.Paths);
        Assert.Empty(empty.Paths);
    }

    [Fact]
    public void Projection_RejectsWhatIsNotAMemberPath()
    {
        Assert.Throws<ArgumentException>(() => new GridProjection<TestOrder>().Load(o => o.Name!.ToUpperInvariant()));
        Assert.Throws<ArgumentException>(() => new GridProjection<TestOrder>().Load("NoSuchMember"));
    }
}

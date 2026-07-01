using System.Linq.Expressions;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GridPropertyExpressionBuilderTests
{
    [Fact]
    public void BuildPropertyExpression_SimpleProperty_ReturnsMemberAccess()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        var property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "Name", nullDefault: null);

        var compiled = Expression.Lambda<Func<TestOrder, string?>>(property, parameter).Compile();
        Assert.Equal("Acme", compiled(new TestOrder { Name = "Acme" }));
    }

    [Fact]
    public void BuildPropertyExpression_NestedPath_WalksNavigation()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        var property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "Vendor.Code", nullDefault: null);

        var compiled = Expression.Lambda<Func<TestOrder, string?>>(property, parameter).Compile();
        Assert.Equal("V1", compiled(new TestOrder { Vendor = new TestVendor { Code = "V1" } }));
    }

    [Fact]
    public void BuildPropertyExpression_NullableWithNullDefault_CoalescesNullToDefault()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        var property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "Approved", nullDefault: false);

        var compiled = Expression.Lambda<Func<TestOrder, bool>>(property, parameter).Compile();

        Assert.False(compiled(new TestOrder { Approved = null }));
        Assert.False(compiled(new TestOrder { Approved = false }));
        Assert.True(compiled(new TestOrder { Approved = true }));
    }

    [Fact]
    public void BuildPropertyExpression_UnknownProperty_Throws()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        Assert.ThrowsAny<Exception>(() =>
            GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "DoesNotExist", nullDefault: null));
    }
}

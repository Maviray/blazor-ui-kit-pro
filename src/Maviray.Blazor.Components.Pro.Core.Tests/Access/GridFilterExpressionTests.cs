using Maviray.Blazor.Components.Pro.Core.Access;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using System.Linq.Expressions;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Access;

public class GridFilterExpressionTests
{
    private static List<Expression> Flatten(Expression expression)
    {
        var collector = new NodeCollector();
        collector.Visit(expression);
        return collector.Nodes;
    }

    private sealed class NodeCollector : ExpressionVisitor
    {
        public List<Expression> Nodes { get; } = [];

        public override Expression? Visit(Expression? node)
        {
            if (node != null)
            {
                Nodes.Add(node);
            }

            return base.Visit(node);
        }
    }

    private static readonly OwnedItem[] Items = [new() { Id = 1, OwnerId = 3 }, new() { Id = 2, OwnerId = 7 }];

    [Fact]
    public void ForInterface_RebindsToTheDataItemProperty_WithNoInterfaceCast()
    {
        var rebound = GridFilterExpression.ForInterface<OwnedItem, IOwnedItem>(x => x.OwnerId == 7);

        var member = Assert.IsType<MemberExpression>(((BinaryExpression)rebound.Body).Left, exactMatch: false);
        Assert.Same(rebound.Parameters[0], member.Expression);
        Assert.Equal(typeof(OwnedItem), member.Member.DeclaringType);
        Assert.DoesNotContain(Flatten(rebound), e => e.NodeType is ExpressionType.Convert or ExpressionType.TypeAs);
        Assert.Equal([2], Items.AsQueryable().Where(rebound).Select(x => x.Id));
    }

    [Fact]
    public void ForInterface_CapturedVariable_StillFilters()
    {
        var ownerId = 7;

        var rebound = GridFilterExpression.ForInterface<OwnedItem, IOwnedItem>(x => x.OwnerId == ownerId);

        Assert.Equal([2], Items.AsQueryable().Where(rebound).Select(x => x.Id));
    }

    [Fact]
    public void ForInterface_NonGenericOverload_ReturnsATypedLambda()
    {
        Expression<Func<IOwnedItem, bool>> filter = x => x.OwnerId == 7;

        var rebound = GridFilterExpression.ForInterface(typeof(OwnedItem), filter);

        Assert.IsType<Expression<Func<OwnedItem, bool>>>(rebound, exactMatch: false);
    }

    [Fact]
    public void ForInterface_MemberImplementedOnABaseClass_BindsToTheBaseProperty()
    {
        var rebound = GridFilterExpression.ForInterface<DerivedOwnedItem, IOwnedItem>(x => x.OwnerId == 7);

        var member = Assert.IsType<MemberExpression>(((BinaryExpression)rebound.Body).Left, exactMatch: false);
        Assert.Same(rebound.Parameters[0], member.Expression);
        Assert.DoesNotContain(Flatten(rebound), e => e.NodeType is ExpressionType.Convert or ExpressionType.TypeAs);
        Assert.Equal([2], new[] { new DerivedOwnedItem { Id = 1, OwnerId = 3 }, new DerivedOwnedItem { Id = 2, OwnerId = 7 } }
            .AsQueryable().Where(rebound).Select(x => x.Id));
    }

    [Fact]
    public void ForInterface_ExplicitImplementation_ThrowsNamingTheMember()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => GridFilterExpression.ForInterface<ExplicitOwnedItem, IOwnedItem>(x => x.OwnerId == 7));

        Assert.Contains("OwnerId", ex.Message);
    }

    [Fact]
    public void ForInterface_TypeNotImplementingTheInterface_ThrowsArgumentException()
    {
        Expression<Func<IOwnedItem, bool>> filter = x => x.OwnerId == 7;

        Assert.Throws<ArgumentException>(() => GridFilterExpression.ForInterface(typeof(TestOrder), filter));
    }
}

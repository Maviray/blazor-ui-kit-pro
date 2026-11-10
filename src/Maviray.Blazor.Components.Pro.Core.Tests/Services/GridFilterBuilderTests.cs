using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using System.Linq.Expressions;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GridFilterBuilderTests
{
    private static readonly TestOrder[] Orders =
    [
        new() { Id = 1, Name = "Alpha", Approved = false, Status = TestOrderStatus.Open, CreatedOn = new DateTime(2026, 1, 1), Vendor = new TestVendor { Code = "A" } },
        new() { Id = 2, Name = null, Approved = null, Status = TestOrderStatus.Closed, CreatedOn = new DateTime(2026, 2, 1), Vendor = new TestVendor { Code = "B" } },
        new() { Id = 3, Name = "Beta", Approved = true, Status = TestOrderStatus.Closed, CreatedOn = new DateTime(2026, 3, 1), Vendor = new TestVendor { Code = "C" } }
    ];

    private static int[] Match(string path, MaviGridFilterCriteria criteria, string value, object? nullDefault = null) =>
        Orders.AsQueryable().Where(GridFilterBuilder<TestOrder>.Build(path, criteria, value, nullDefault)).Select(x => x.Id).ToArray();

    [Theory]
    [InlineData("Id", MaviGridFilterCriteria.Equals, "2", new[] { 2 })]
    [InlineData("Id", MaviGridFilterCriteria.NotEquals, "2", new[] { 1, 3 })]
    [InlineData("Id", MaviGridFilterCriteria.GreaterThan, "1", new[] { 2, 3 })]
    [InlineData("Id", MaviGridFilterCriteria.LessThanOrEqual, "2", new[] { 1, 2 })]
    [InlineData("Status", MaviGridFilterCriteria.Equals, "Closed", new[] { 2, 3 })]
    [InlineData("Status", MaviGridFilterCriteria.Equals, "0", new[] { 1 })]
    [InlineData("CreatedOn", MaviGridFilterCriteria.Before, "2026-02-01", new[] { 1 })]
    [InlineData("CreatedOn", MaviGridFilterCriteria.After, "2026-02-01", new[] { 3 })]
    [InlineData("Vendor.Code", MaviGridFilterCriteria.Equals, "C", new[] { 3 })]
    [InlineData("Approved", MaviGridFilterCriteria.IsTrue, "true", new[] { 3 })]
    [InlineData("Approved", MaviGridFilterCriteria.IsFalse, "false", new[] { 1 })]
    public void Build_MatchesTheExpectedRows(string path, MaviGridFilterCriteria criteria, string value, int[] expected)
    {
        Assert.Equal(expected, Match(path, criteria, value));
    }

    [Fact]
    public void Build_Contains_SkipsNullValues()
    {
        Assert.Equal([1, 3], Match("Name", MaviGridFilterCriteria.Contains, "a"));
    }

    [Fact]
    public void Build_Excludes_KeepsNullValues()
    {
        Assert.Equal([2, 3], Match("Name", MaviGridFilterCriteria.Excludes, "Alpha"));
    }

    [Fact]
    public void Build_WithNullDefault_TreatsNullAsTheDefault()
    {
        Assert.Equal([1, 2], Match("Approved", MaviGridFilterCriteria.IsFalse, "false", nullDefault: false));
    }

    [Theory]
    [InlineData("Id", MaviGridFilterCriteria.Equals, "42", 42)]
    [InlineData("Id", MaviGridFilterCriteria.GreaterThan, "42", 42)]
    [InlineData("Name", MaviGridFilterCriteria.Contains, "abc", "abc")]
    [InlineData("Name", MaviGridFilterCriteria.Excludes, "abc", "abc")]
    public void Build_DoesNotEmbedTheFilterValueAsAConstant(string path, MaviGridFilterCriteria criteria, string value, object coerced)
    {
        var filter = GridFilterBuilder<TestOrder>.Build(path, criteria, value, null);

        Assert.DoesNotContain(ConstantCollector.Collect(filter), c => Equals(c.Value, coerced));
    }

    [Fact]
    public void Build_ValueThatDoesNotFitTheProperty_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => GridFilterBuilder<TestOrder>.Build("Id", MaviGridFilterCriteria.Equals, "abc", null));
    }

    [Fact]
    public void Build_NoCriteria_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GridFilterBuilder<TestOrder>.Build("Id", MaviGridFilterCriteria.None, "1", null));
    }

    private sealed class ConstantCollector : ExpressionVisitor
    {
        private readonly List<ConstantExpression> _constants = [];

        public static List<ConstantExpression> Collect(Expression expression)
        {
            var collector = new ConstantCollector();
            collector.Visit(expression);
            return collector._constants;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _constants.Add(node);
            return base.VisitConstant(node);
        }
    }
}

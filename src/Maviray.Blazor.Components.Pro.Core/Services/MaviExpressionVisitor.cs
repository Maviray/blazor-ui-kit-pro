using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Services;

public class MaviExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _newValue;
    private readonly Expression _oldValue;

    public MaviExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue;
        _newValue = newValue;
    }

    protected override Expression VisitParameter(ParameterExpression node) => node == _oldValue ? _newValue : base.VisitParameter(node);
}
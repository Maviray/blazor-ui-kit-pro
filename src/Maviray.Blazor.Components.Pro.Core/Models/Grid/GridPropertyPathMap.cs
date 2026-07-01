using System.Linq.Expressions;
using System.Reflection;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// Maps a <typeparamref name="TModel"/> property built from custom logic in
/// <c>IGridCollectionModel&lt;TDataItem,TKey&gt;.ConstructFrom</c> to the <typeparamref name="TDataItem"/>
/// dot-path <see cref="Services.GenericGridDataFactory{TDataItem,TModel,TKey}"/> should use to sort/filter
/// by that property. Optionally carries a <c>nullDefault</c> so a nullable navigation value that
/// <c>ConstructFrom</c> coalesces (e.g. <c>dataItem.Nav?.Flag ?? false</c>) sorts/filters the same way at
/// the database as it does in memory.
/// </summary>
public sealed class GridPropertyPathMap<TDataItem, TModel>
    where TDataItem : class
{
    private readonly Dictionary<string, GridPropertyPath> _paths = [];

    public GridPropertyPathMap<TDataItem, TModel> Map(
        Expression<Func<TModel, object?>> modelProperty,
        string dataItemPath,
        object? nullDefault = null)
    {
        _paths[GetPropertyName(modelProperty)] = new GridPropertyPath(dataItemPath, nullDefault);
        return this;
    }

    internal bool TryResolve(string modelPropertyName, out GridPropertyPath path) =>
        _paths.TryGetValue(modelPropertyName, out path!);

    private static string GetPropertyName(Expression<Func<TModel, object?>> modelProperty)
    {
        var body = modelProperty.Body;

        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression { Member: PropertyInfo property })
        {
            return property.Name;
        }

        throw new ArgumentException($"Expression '{modelProperty}' must be a simple property access.", nameof(modelProperty));
    }
}

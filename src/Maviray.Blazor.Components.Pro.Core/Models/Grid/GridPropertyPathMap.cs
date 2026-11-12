using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// Data item paths for model columns that have no identically named data item member, typically columns computed
/// in <c>ConstructFrom</c>. A mapped column can be sorted and filtered at the data source. Immutable:
/// <see cref="Map"/> returns a new map, so a map can be shared and the columns built for it are reused.
/// </summary>
public sealed class GridPropertyPathMap<TDataItem, TModel> where TDataItem : class
{
    private readonly ImmutableDictionary<string, GridPropertyPath> _paths;

    public GridPropertyPathMap() : this(ImmutableDictionary.Create<string, GridPropertyPath>(StringComparer.Ordinal))
    {
    }

    private GridPropertyPathMap(ImmutableDictionary<string, GridPropertyPath> paths) => _paths = paths;

    /// <summary>Returns a map in which <paramref name="modelProperty"/> sorts and filters by <paramref name="dataItemPath"/>.</summary>
    /// <param name="modelProperty">The model property, for example <c>m => m.CustomerName</c>.</param>
    /// <param name="dataItemPath">Dot-path on the data item, for example <c>"Customer.Name"</c>.</param>
    /// <param name="nullDefault">
    /// When set, a <see langword="null"/> at the path sorts and filters as this value, matching a coalesce in
    /// <c>ConstructFrom</c> such as <c>?? false</c>.
    /// </param>
    public GridPropertyPathMap<TDataItem, TModel> Map(Expression<Func<TModel, object?>> modelProperty, string dataItemPath, object? nullDefault = null)
    {
        ArgumentNullException.ThrowIfNull(modelProperty);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataItemPath);

        return new GridPropertyPathMap<TDataItem, TModel>(_paths.SetItem(GetPropertyName(modelProperty), new GridPropertyPath(dataItemPath, nullDefault)));
    }

    internal bool TryResolve(string modelPropertyName, out GridPropertyPath path) => _paths.TryGetValue(modelPropertyName, out path!);

    private static string GetPropertyName(Expression<Func<TModel, object?>> modelProperty)
    {
        var body = modelProperty.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary ? unary.Operand : modelProperty.Body;

        return body is MemberExpression { Member: PropertyInfo property }
            ? property.Name
            : throw new ArgumentException($"Expression '{modelProperty}' must be a simple property access.", nameof(modelProperty));
    }
}

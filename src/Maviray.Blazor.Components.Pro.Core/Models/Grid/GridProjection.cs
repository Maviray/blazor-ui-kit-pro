using Maviray.Blazor.Components.Pro.Core.Services;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// Loads only the data item members a grid needs instead of whole entities with their includes: the paths of the
/// visible columns, plus the members listed here. Set it as <see cref="GridFetchOptions{TDataItem, TModel}.Projection"/>.
/// Immutable: <see cref="Load{TValue}"/> returns a new projection.
/// </summary>
/// <remarks>
/// Each page holds new data item instances with only those members set (and only those members of related items);
/// every other member has its default value. List every member <c>ConstructFrom</c> and <c>GetContextActions</c> read
/// that isn't a column path, including the one the row key comes from. The data item type and every related type on
/// a path need a parameterless constructor and settable members.
/// </remarks>
public sealed class GridProjection<TDataItem> where TDataItem : class
{
    private readonly ImmutableArray<string> _paths;

    public GridProjection() : this(ImmutableArray<string>.Empty)
    {
    }

    private GridProjection(ImmutableArray<string> paths) => _paths = paths;

    /// <summary>The members to load besides the column paths, as dot-paths.</summary>
    public IReadOnlyList<string> Paths => _paths;

    /// <summary>Also loads <paramref name="member"/>, for example <c>o =&gt; o.Customer!.Name</c>.</summary>
    /// <exception cref="ArgumentException">The expression isn't a chain of member accesses on the data item.</exception>
    public GridProjection<TDataItem> Load<TValue>(Expression<Func<TDataItem, TValue>> member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return Load(PathOf(member));
    }

    /// <summary>Also loads the member at <paramref name="path"/>, a dot-path such as <c>"Customer.Name"</c>.</summary>
    /// <exception cref="ArgumentException">The path doesn't name public members of the data item.</exception>
    public GridProjection<TDataItem> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return GridDataItemPaths<TDataItem>.Exists(path)
            ? new GridProjection<TDataItem>(_paths.Add(path))
            : throw new ArgumentException($"'{path}' isn't a path of public members of {typeof(TDataItem).Name}.", nameof(path));
    }

    private static string PathOf(LambdaExpression member)
    {
        var segments = new Stack<string>();
        var node = member.Body;

        while (true)
        {
            switch (node)
            {
                case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert:
                    node = convert.Operand;
                    continue;
                case MemberExpression access when access.Expression is not null:
                    segments.Push(access.Member.Name);
                    node = access.Expression;
                    continue;
                case ParameterExpression when segments.Count > 0:
                    return string.Join('.', segments);
                default:
                    throw new ArgumentException($"'{member}' must be a chain of member accesses, such as o => o.Customer.Name.", nameof(member));
            }
        }
    }
}

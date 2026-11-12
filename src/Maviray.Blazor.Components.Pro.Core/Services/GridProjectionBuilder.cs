using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>
/// Builds <c>x =&gt; new T { A = x.A, Nav = x.Nav == null ? null : new Nav { B = x.Nav.B } }</c> for a set of dot-paths,
/// once per set. A query provider such as EF Core translates it into a select of just those columns (and joins).
/// </summary>
internal static class GridProjectionBuilder<T> where T : class
{
    private static readonly ConcurrentDictionary<string, Expression<Func<T, T>>> Cache = new(StringComparer.Ordinal);

    public static Expression<Func<T, T>> For(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var distinct = paths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        if (distinct.Length == 0)
        {
            throw new ArgumentException("A projection needs at least one path.", nameof(paths));
        }

        return Cache.GetOrAdd(string.Join('|', distinct), static (_, p) => Build(p), distinct);
    }

    private static Expression<Func<T, T>> Build(string[] paths)
    {
        var root = new Node();

        foreach (var path in paths)
        {
            var node = root;

            foreach (var segment in path.Split('.'))
            {
                node = node.Child(segment);
            }

            node.Whole = true;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        return Expression.Lambda<Func<T, T>>(Init(parameter, typeof(T), root), parameter);
    }

    private static MemberInitExpression Init(Expression source, Type type, Node node)
    {
        var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes)
            ?? throw new InvalidOperationException($"{type.Name} needs a parameterless constructor to be loaded member by member.");

        var bindings = new List<MemberBinding>(node.Children.Count);

        foreach (var (name, child) in node.Children)
        {
            MemberInfo member = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                ?? (MemberInfo?)type.GetField(name, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new ArgumentException($"'{name}' isn't a public member of {type.Name}.");

            if (member is PropertyInfo { CanWrite: false } || member is FieldInfo { IsInitOnly: true })
            {
                throw new InvalidOperationException($"{type.Name}.{name} can't be set, so it can't be loaded member by member.");
            }

            var access = Expression.MakeMemberAccess(source, member);

            // A path to the member itself loads all of it; a path through it loads only what's below.
            Expression value = child.Whole ? access : Related(access, Init(access, access.Type, child));

            bindings.Add(Expression.Bind(member, value));
        }

        return Expression.MemberInit(Expression.New(constructor), bindings);
    }

    // A missing related item stays null instead of becoming an empty one.
    private static Expression Related(Expression access, Expression init) =>
        access.Type.IsValueType
            ? init
            : Expression.Condition(Expression.Equal(access, Expression.Constant(null, access.Type)), Expression.Constant(null, access.Type), init);

    private sealed class Node
    {
        public Dictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);

        public bool Whole { get; set; }

        public Node Child(string name)
        {
            if (!Children.TryGetValue(name, out var child))
            {
                Children[name] = child = new Node();
            }

            return child;
        }
    }
}

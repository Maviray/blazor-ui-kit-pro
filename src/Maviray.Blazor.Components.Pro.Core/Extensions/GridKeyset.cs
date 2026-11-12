using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Services;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

/// <summary>One key of a keyset order: a dot-path, its direction, and the value a <see langword="null"/> at the path sorts as.</summary>
public sealed record GridOrderKey(string Path, SortOrder Order, object? NullDefault = null);

/// <summary>
/// Keyset ("seek") paging over an order known only at run time, for grid fetch code over <see cref="IQueryable{T}"/>
/// (EF Core, or an app's own query code). It orders the query and, given a <see cref="MaviGridCursor"/>, loads the
/// rows just after or before it with a <c>WHERE</c> on the order keys instead of skipping rows, so with an index on
/// the keys every page costs the same however deep it is. Without a usable cursor it skips and takes as usual.
/// </summary>
/// <remarks>
/// <para>The keys must make the order total: end with a unique key, such as the primary key.</para>
/// <para>Every key's value must be non-null in the store (give a nullable path a null default) and of a type the
/// store compares the way it orders (see <see cref="CanSeek"/>).</para>
/// <para>Seek values are sent as query parameters. A cursor written for another order, or one that can't be read,
/// is ignored rather than trusted. A cursor holds the key values of a row the user has already seen.</para>
/// </remarks>
public sealed class GridKeyset<T> where T : class
{
    private static readonly MethodInfo StringCompare = typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;
    private static readonly MethodInfo GuidCompareTo = typeof(Guid).GetMethod(nameof(Guid.CompareTo), [typeof(Guid)])!;

    private static readonly FrozenSet<Type> SeekableTypes = new[]
    {
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(bool), typeof(Guid),
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan)
    }.ToFrozenSet();

    // Compiling the getters is the costly part; a data source builds a keyset per fetch, so share them per order.
    private static readonly ConcurrentDictionary<string, Compiled> Cache = new(StringComparer.Ordinal);

    private readonly Compiled _compiled;

    /// <exception cref="ArgumentException">There are no keys, a path doesn't exist, or a key's type can't be sought.</exception>
    public GridKeyset(IReadOnlyList<GridOrderKey> keys)
    {
        _compiled = Compile(keys, out var error) ?? throw new ArgumentException(error, nameof(keys));
    }

    private GridKeyset(Compiled compiled) => _compiled = compiled;

    public IReadOnlyList<GridOrderKey> Keys => _compiled.Keys;

    /// <summary>
    /// Whether a key of <paramref name="type"/> can be sought: a number, <see cref="string"/>, <see cref="bool"/>,
    /// <see cref="Guid"/>, enum, date or time. Nullable types can't (NULLs don't compare); give the key a null default.
    /// </summary>
    /// <remarks>
    /// Values are compared by the store, the same way it orders them, so a store-specific order (SQL Server's
    /// <c>uniqueidentifier</c> byte order, a column's collation) is followed. An enum compares by its underlying number,
    /// so it must be stored as a number (no value converter to text).
    /// </remarks>
    public static bool CanSeek(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return SeekableTypes.Contains(type) || type.IsEnum;
    }

    /// <summary>Creates a keyset, or returns <see langword="false"/> when the keys can't be sought (see <see cref="CanSeek"/>).</summary>
    public static bool TryCreate(IReadOnlyList<GridOrderKey> keys, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GridKeyset<T>? keyset)
    {
        var compiled = Compile(keys, out _);
        keyset = compiled is null ? null : new GridKeyset<T>(compiled);
        return keyset is not null;
    }

    /// <summary>
    /// Loads one page: with a usable <paramref name="cursor"/>, the <paramref name="take"/> rows after or before it;
    /// otherwise <paramref name="skip"/> rows in and <paramref name="take"/> rows on. Rows come back in the keyset's order.
    /// </summary>
    /// <param name="source">The filtered rows to page through, unordered.</param>
    /// <param name="cursor">The grid's <see cref="MaviGridQuery.Cursor"/>; <see langword="null"/>, from another order,
    /// or unreadable means skip instead.</param>
    /// <param name="skip">Rows to skip when not seeking: <c>(PageNumber - 1) * PageSize</c>.</param>
    /// <param name="take">The page size.</param>
    /// <param name="load">Runs the query, for example <c>(q, ct) =&gt; q.ToListAsync(ct)</c> with EF Core. It may add a
    /// projection, as long as the projected rows still carry the order keys.</param>
    /// <param name="cancellationToken">Passed to <paramref name="load"/>.</param>
    public async Task<GridKeysetPage<T>> LoadPageAsync(
        IQueryable<T> source,
        MaviGridCursor? cursor,
        int skip,
        int take,
        Func<IQueryable<T>, CancellationToken, Task<List<T>>> load,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(load);

        List<T> items;

        if (cursor is not null && TryReadCursor(cursor.Value, out var values))
        {
            // Before the cursor: walk the order backwards from it, then put the page back in order.
            var backward = cursor.Direction == MaviGridCursorDirection.Previous;
            items = await load(Order(source.Where(Seek(values, backward)), backward).Take(take), cancellationToken).ConfigureAwait(false);

            if (backward)
            {
                items.Reverse();
            }
        }
        else
        {
            items = await load(Order(source, reverse: false).Skip(skip).Take(take), cancellationToken).ConfigureAwait(false);
        }

        return items.Count == 0
            ? new GridKeysetPage<T>(items, null, null)
            : new GridKeysetPage<T>(items, CursorFor(items[0]), CursorFor(items[^1]));
    }

    /// <summary>The cursor of <paramref name="item"/>, or <see langword="null"/> when a key's path can't be read from it.</summary>
    public string? CursorFor(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var values = new JsonElement[_compiled.Getters.Length];

        for (var i = 0; i < values.Length; i++)
        {
            object? value;

            try
            {
                value = _compiled.Getters[i](item);
            }
            catch (NullReferenceException)
            {
                // A navigation on the path wasn't loaded: there's nothing to seek from.
                return null;
            }

            if (value is null)
            {
                return null;
            }

            values[i] = JsonSerializer.SerializeToElement(value, _compiled.Members[i].Type);
        }

        return JsonSerializer.Serialize(new CursorPayload(_compiled.Signature, values));
    }

    private bool TryReadCursor(string text, out object[] values)
    {
        values = [];

        try
        {
            var payload = JsonSerializer.Deserialize<CursorPayload>(text);

            if (payload?.Values is null || payload.Order != _compiled.Signature || payload.Values.Length != _compiled.Members.Length)
            {
                return false;
            }

            var read = new object[payload.Values.Length];

            for (var i = 0; i < read.Length; i++)
            {
                if (payload.Values[i].Deserialize(_compiled.Members[i].Type) is not { } value)
                {
                    return false;
                }

                read[i] = value;
            }

            values = read;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    private IQueryable<T> Order(IQueryable<T> source, bool reverse)
    {
        var expression = source.Expression;

        for (var i = 0; i < _compiled.Keys.Count; i++)
        {
            var ascending = (_compiled.Keys[i].Order == SortOrder.Ascending) != reverse;
            var method = (i == 0, ascending) switch
            {
                (true, true) => nameof(Queryable.OrderBy),
                (true, false) => nameof(Queryable.OrderByDescending),
                (false, true) => nameof(Queryable.ThenBy),
                (false, false) => nameof(Queryable.ThenByDescending)
            };

            var member = _compiled.Members[i];
            expression = Expression.Call(typeof(Queryable), method, [typeof(T), member.Type], expression,
                Expression.Quote(Expression.Lambda(member, _compiled.Parameter)));
        }

        return source.Provider.CreateQuery<T>(expression);
    }

    // After (k1, k2, k3) = (v1, v2, v3) in ascending order:
    //   k1 >= v1 AND (k1 > v1 OR (k1 = v1 AND k2 > v2) OR (k1 = v1 AND k2 = v2 AND k3 > v3))
    // Descending keys compare the other way, and so does seeking backwards. The leading range lets the store seek an
    // index on the first key instead of scanning.
    private Expression<Func<T, bool>> Seek(object[] values, bool backward)
    {
        var members = _compiled.Members;
        var bound = new Expression[members.Length];

        for (var i = 0; i < members.Length; i++)
        {
            bound[i] = Parameterized(values[i], members[i].Type);
        }

        bool After(int i) => (_compiled.Keys[i].Order == SortOrder.Ascending) != backward;

        Expression? any = null;

        for (var i = 0; i < members.Length; i++)
        {
            var term = Compare(members[i], values[i], bound[i], After(i) ? ExpressionType.GreaterThan : ExpressionType.LessThan);

            for (var j = i - 1; j >= 0; j--)
            {
                term = Expression.AndAlso(Expression.Equal(members[j], bound[j]), term);
            }

            any = any is null ? term : Expression.OrElse(any, term);
        }

        var body = members.Length == 1
            ? any!
            : Expression.AndAlso(Compare(members[0], values[0], bound[0], After(0) ? ExpressionType.GreaterThanOrEqual : ExpressionType.LessThanOrEqual), any!);

        return Expression.Lambda<Func<T, bool>>(body, _compiled.Parameter);
    }

    // Strings and Guids have no comparison operators: Compare / CompareTo against 0 translates to a > b in SQL. Enums
    // compare by their underlying number.
    private static Expression Compare(Expression member, object value, Expression bound, ExpressionType comparison)
    {
        var type = member.Type;

        if (type == typeof(bool))
        {
            return CompareBool(member, (bool)value, comparison);
        }

        if (type.IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(type);
            return Expression.MakeBinary(comparison, Expression.Convert(member, underlying), Expression.Convert(bound, underlying));
        }

        var compared = type == typeof(string) ? Expression.Call(StringCompare, member, bound)
            : type == typeof(Guid) ? Expression.Call(member, GuidCompareTo, bound)
            : null;

        return compared is null
            ? Expression.MakeBinary(comparison, member, bound)
            : Expression.MakeBinary(comparison, compared, Expression.Constant(0));
    }

    // bool has no ordering operators either. false sorts before true, so with the cursor's value known each
    // comparison is the column itself, its negation, or a constant: b > false is b, b < true is !b.
    private static Expression CompareBool(Expression member, bool value, ExpressionType comparison) => comparison switch
    {
        ExpressionType.GreaterThan => value ? Expression.Constant(false) : member,
        ExpressionType.GreaterThanOrEqual => value ? member : Expression.Constant(true),
        ExpressionType.LessThan => value ? Expression.Not(member) : Expression.Constant(false),
        ExpressionType.LessThanOrEqual => value ? Expression.Constant(true) : Expression.Not(member),
        _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null)
    };

    // A captured value rather than a constant, so a relational provider sends it as a parameter.
    private static Expression Parameterized(object value, Type type) =>
        Expression.Convert(Expression.Field(Expression.Constant(new StrongBox<object?>(value)), nameof(StrongBox<object>.Value)), type);

    private static Compiled? Compile(IReadOnlyList<GridOrderKey>? keys, out string? error)
    {
        if (keys is null || keys.Count == 0)
        {
            error = "A keyset needs at least one order key.";
            return null;
        }

        var signature = string.Join(",", keys.Select(k => k.Path + (k.Order == SortOrder.Ascending ? "+" : "-")));
        var cacheKey = signature + "|" + string.Join("|", keys.Select(k => k.NullDefault is null ? "" : $"{k.NullDefault.GetType().FullName}:{Convert.ToString(k.NullDefault, CultureInfo.InvariantCulture)}"));

        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            error = null;
            return cached;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var members = new Expression[keys.Count];
        var getters = new Func<T, object?>[keys.Count];

        for (var i = 0; i < keys.Count; i++)
        {
            try
            {
                members[i] = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, keys[i].Path, keys[i].NullDefault);
            }
            catch (ArgumentException)
            {
                error = $"'{keys[i].Path}' isn't a path on {typeof(T).Name}.";
                return null;
            }

            if (!CanSeek(members[i].Type))
            {
                error = $"'{keys[i].Path}' is a {members[i].Type.Name}, which can't be sought; use numbers, strings, bools, Guids, enums, dates or times (nullable values need a null default).";
                return null;
            }

            getters[i] = Expression.Lambda<Func<T, object?>>(Expression.Convert(members[i], typeof(object)), parameter).Compile();
        }

        error = null;
        return Cache.GetOrAdd(cacheKey, new Compiled([.. keys], signature, parameter, members, getters));
    }

    private sealed record Compiled(
        IReadOnlyList<GridOrderKey> Keys, string Signature, ParameterExpression Parameter, Expression[] Members, Func<T, object?>[] Getters);

    private sealed record CursorPayload(
        [property: JsonPropertyName("o")] string? Order,
        [property: JsonPropertyName("v")] JsonElement[]? Values);
}

using Mapster;
using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>
/// Everything the factory needs to know about a grid model, worked out once per model type: the columns and their
/// formatting, compiled property getters, the navigations to include, and the data item to model mapping. Column
/// lists are immutable and cached per path map, so fetching a page allocates only rows and cells.
/// </summary>
internal sealed class GridModelMetadata<TDataItem, TModel>
    where TDataItem : class
    where TModel : class
{
    private static readonly Lazy<GridModelMetadata<TDataItem, TModel>> LazyInstance = new(() => new GridModelMetadata<TDataItem, TModel>());

    private readonly GridColumnSet _unmapped;
    private readonly ConditionalWeakTable<GridPropertyPathMap<TDataItem, TModel>, GridColumnSet> _mapped = new();

    private GridModelMetadata()
    {
        (Descriptors, IncludePaths) = Discover();
        DeniedColumns = Descriptors.Select((d, i) => d.ToColumn(i, resolvable: false)).ToArray();
        _unmapped = BuildColumnSet(null);
        MapToModel = BuildMapping();
    }

    public static GridModelMetadata<TDataItem, TModel> Instance => LazyInstance.Value;

    /// <summary>Column descriptors in display order.</summary>
    public IReadOnlyList<GridColumnDescriptor> Descriptors { get; }

    public IReadOnlyList<string> IncludePaths { get; }

    /// <summary>The columns of a denied page: same layout, nothing sortable or filterable.</summary>
    public IReadOnlyList<MaviGridColumn> DeniedColumns { get; }

    public Func<TDataItem, TModel> MapToModel { get; }

    public GridColumnSet ColumnsFor(GridPropertyPathMap<TDataItem, TModel>? map) =>
        map is null ? _unmapped : _mapped.GetValue(map, m => BuildColumnSet(m));

    private GridColumnSet BuildColumnSet(GridPropertyPathMap<TDataItem, TModel>? map)
    {
        var columns = new MaviGridColumn[Descriptors.Count];
        var paths = new GridPropertyPath?[Descriptors.Count];

        for (var i = 0; i < Descriptors.Count; i++)
        {
            paths[i] = Resolve(Descriptors[i].Key, map);
            columns[i] = Descriptors[i].ToColumn(i, resolvable: paths[i] is not null);
        }

        return new GridColumnSet(columns, paths, [.. Enumerable.Range(0, Descriptors.Count)]);
    }

    private static GridPropertyPath? Resolve(string key, GridPropertyPathMap<TDataItem, TModel>? map)
    {
        if (map is not null && map.TryResolve(key, out var mapped))
        {
            return GridDataItemPaths<TDataItem>.Exists(mapped.DataItemPath) ? mapped : null;
        }

        return GridDataItemPaths<TDataItem>.Exists(key) ? new GridPropertyPath(key, null) : null;
    }

    private static (GridColumnDescriptor[] Descriptors, string[] IncludePaths) Discover()
    {
        var descriptors = new List<GridColumnDescriptor>();
        var includes = new List<string>();

        foreach (var property in typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<GridDataPropertyAttribute>() is { } data)
            {
                descriptors.Add(Describe(property, property.Name, DisplayName(property) ?? property.Name, data.Sequence, data.CellBehavior, navigation: null));
            }
            else if (property.GetCustomAttribute<GridNavPropertyAttribute>() is { } navigation)
            {
                includes.Add(property.Name);
                var navigationGetter = CompileGetter(property);

                foreach (var nested in property.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (nested.GetCustomAttribute<GridNestedPropertyAttribute>() is { } nestedColumn)
                    {
                        descriptors.Add(Describe(nested, $"{property.Name}.{nested.Name}", nestedColumn.Title, navigation.Sequence, navigation.CellBehavior, navigationGetter));
                    }
                }
            }
        }

        // OrderBy is stable, so columns with equal sequences keep their declaration order.
        return (descriptors.OrderBy(d => d.Sequence).ToArray(), includes.ToArray());
    }

    private static GridColumnDescriptor Describe(
        PropertyInfo property, string key, string title, int sequence, MaviGridColumnCellBehavior behavior, Func<object, object?>? navigation)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var booleans = property.GetCustomAttribute<GridBooleanDisplayValuesAttribute>();
        var (enumOptions, enumTexts) = type.IsEnum ? DescribeEnum(type) : ([], null);

        return new GridColumnDescriptor
        {
            Key = key,
            Title = title,
            Sequence = sequence,
            CellBehavior = behavior,
            PropertyType = Categorize(type),
            GraphicDisplay = property.GetCustomAttribute<GridAdvancedDisplayAttribute>() is not null,
            TrueText = booleans?.TrueText ?? bool.TrueString,
            FalseText = booleans?.FalseText ?? bool.FalseString,
            DateTimeFormat = property.GetCustomAttribute<GridDateTimeFormatAttribute>()?.Format,
            EnumOptions = enumOptions,
            EnumTexts = enumTexts,
            NavigationGetter = navigation,
            ValueGetter = CompileGetter(property)
        };
    }

    private static (MaviGridEnumOption[] Options, FrozenDictionary<object, string> Texts) DescribeEnum(Type enumType)
    {
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        var options = new MaviGridEnumOption[fields.Length];
        var texts = new Dictionary<object, string>();

        for (var i = 0; i < fields.Length; i++)
        {
            var text = fields[i].GetCustomAttribute<DisplayAttribute>()?.GetName() ?? fields[i].Name;
            options[i] = new MaviGridEnumOption(fields[i].Name, text);
            texts.TryAdd(fields[i].GetValue(null)!, text);
        }

        return (options, texts.ToFrozenDictionary());
    }

    private static string? DisplayName(PropertyInfo property) => property.GetCustomAttribute<DisplayAttribute>()?.GetName();

    private static MaviGridPropertyType Categorize(Type type) => type switch
    {
        _ when type == typeof(string) => MaviGridPropertyType.String,
        _ when type == typeof(char) => MaviGridPropertyType.Char,
        _ when type == typeof(bool) => MaviGridPropertyType.Bool,
        _ when type.IsEnum => MaviGridPropertyType.Enum,
        _ when type == typeof(Guid) => MaviGridPropertyType.Guid,
        _ when type == typeof(TimeSpan) => MaviGridPropertyType.TimeSpan,
        _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) => MaviGridPropertyType.DateTime,
        _ when type == typeof(decimal) || type == typeof(double) || type == typeof(float) => MaviGridPropertyType.Decimal,
        _ when type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) => MaviGridPropertyType.Int,
        _ => MaviGridPropertyType.Undefined
    };

    private static Func<object, object?> CompileGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var access = Expression.Property(Expression.Convert(instance, property.DeclaringType!), property);
        return Expression.Lambda<Func<object, object?>>(Expression.Convert(access, typeof(object)), instance).Compile();
    }

    private static Func<TDataItem, TModel> BuildMapping()
    {
        // Eager-loaded navigations can form reference cycles; preserving references maps each object once.
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.Default.PreserveReference(true);
        return config.GetMapFunction<TDataItem, TModel>();
    }
}

/// <summary>
/// The columns built for one path map (less any hidden columns), with the data item path each column sorts and filters
/// by and the descriptor that formats its cells.
/// </summary>
internal sealed class GridColumnSet
{
    private readonly GridPropertyPath?[] _paths;
    private readonly int[] _descriptorIndexes;
    private readonly FrozenDictionary<string, int> _positionByKey;

    // Policies hide the same few column sets again and again; one list per set keeps the grid's layout from rebuilding.
    private readonly ConcurrentDictionary<string, GridColumnSet> _withHidden = new(StringComparer.Ordinal);

    public GridColumnSet(IReadOnlyList<MaviGridColumn> columns, GridPropertyPath?[] paths, int[] descriptorIndexes)
    {
        Columns = columns;
        _paths = paths;
        _descriptorIndexes = descriptorIndexes;
        _positionByKey = columns.Select((c, i) => (c.Key, i)).ToFrozenDictionary(x => x.Key, x => x.i, StringComparer.Ordinal);
    }

    public IReadOnlyList<MaviGridColumn> Columns { get; }

    /// <summary>For each column, in order, the index of the descriptor that reads and formats its cells.</summary>
    public IReadOnlyList<int> DescriptorIndexes => _descriptorIndexes;

    /// <summary>The data item paths of the columns that have one.</summary>
    public IEnumerable<GridPropertyPath> Paths => _paths.OfType<GridPropertyPath>();

    /// <summary>Finds the path of a sortable and filterable column; false for unknown keys and columns without a path.</summary>
    public bool TryGetPath(string? columnKey, [NotNullWhen(true)] out GridPropertyPath? path)
    {
        if (columnKey is not null && _positionByKey.TryGetValue(columnKey, out var index) && _paths[index] is { } resolved)
        {
            path = resolved;
            return true;
        }

        path = null;
        return false;
    }

    /// <summary>The set without <paramref name="hiddenColumns"/>, re-indexed; this set when none of them is a column.</summary>
    public GridColumnSet Without(IReadOnlyCollection<string>? hiddenColumns)
    {
        if (hiddenColumns is null || hiddenColumns.Count == 0)
        {
            return this;
        }

        var hidden = hiddenColumns.Where(_positionByKey.ContainsKey).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        return hidden.Length == 0 ? this : _withHidden.GetOrAdd(string.Join('\u001f', hidden), _ => Filter(hidden.ToHashSet(StringComparer.Ordinal)));
    }

    private GridColumnSet Filter(HashSet<string> hidden)
    {
        var keep = Enumerable.Range(0, Columns.Count).Where(i => !hidden.Contains(Columns[i].Key)).ToArray();

        return new GridColumnSet(
            keep.Select((i, position) => Columns[i].WithIndex(position)).ToArray(),
            keep.Select(i => _paths[i]).ToArray(),
            keep.Select(i => _descriptorIndexes[i]).ToArray());
    }
}

/// <summary>How one column reads and formats its value.</summary>
internal sealed class GridColumnDescriptor
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required int Sequence { get; init; }
    public required MaviGridPropertyType PropertyType { get; init; }
    public required MaviGridColumnCellBehavior CellBehavior { get; init; }
    public required bool GraphicDisplay { get; init; }
    public required string TrueText { get; init; }
    public required string FalseText { get; init; }
    public required string? DateTimeFormat { get; init; }
    public required IReadOnlyList<MaviGridEnumOption> EnumOptions { get; init; }
    public required FrozenDictionary<object, string>? EnumTexts { get; init; }

    /// <summary>For a nested column: reads the related model from the grid model.</summary>
    public required Func<object, object?>? NavigationGetter { get; init; }

    /// <summary>Reads the value from the grid model, or from the related model for a nested column.</summary>
    public required Func<object, object?> ValueGetter { get; init; }

    /// <summary>Reads the value; false when a nested column's navigation is null.</summary>
    public bool TryGetValue(object model, out object? value)
    {
        var owner = NavigationGetter is null ? model : NavigationGetter(model);

        if (owner is null)
        {
            value = null;
            return false;
        }

        value = ValueGetter(owner);
        return true;
    }

    public string Format(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        bool flag => flag ? TrueText : FalseText,
        Enum member => EnumTexts is not null && EnumTexts.TryGetValue(member, out var text) ? text : member.ToString(),
        DateTime dateTime => dateTime.ToString(DateTimeFormat ?? "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset offset => offset.ToString(DateTimeFormat ?? "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString(DateTimeFormat ?? "yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString(DateTimeFormat ?? "HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    public MaviGridColumn ToColumn(int index, bool resolvable) => new()
    {
        Key = Key,
        Title = Title,
        Index = index,
        Sequence = Sequence,
        PropertyType = PropertyType,
        CellBehavior = CellBehavior,
        GraphicDisplay = GraphicDisplay,
        Sortable = resolvable,
        Filterable = resolvable,
        TrueText = TrueText,
        FalseText = FalseText,
        EnumOptions = EnumOptions
    };
}

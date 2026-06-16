using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Constants;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Mapster;

namespace Maviray.Blazor.Components.Pro.Core.Services;

public class GenericGridDataFactory<TDataItem, TModel, TKey> : IGenericGridDataFactory<TDataItem, TModel, TKey>
    where TDataItem : class
    where TModel : class, IGridCollectionModel<TDataItem, TKey>
    where TKey : IEquatable<TKey> 
{
    // Navigation properties on the model that must be eager-loaded from the data source so their
    // nested columns are populated. Computed once per closed generic type via reflection.
    // ReSharper disable once StaticMemberInGenericType
    private static readonly IReadOnlyCollection<string> NavigationIncludePaths = BuildNavigationIncludePaths();

    // Eager-loaded navigations can form reference cycles (e.g. parent.Child.Parent after EF
    // relationship fixup). PreserveReference makes Mapster reuse an already-mapped instance instead
    // of recursing into the cycle, which would otherwise overflow the stack. Cloned from the global
    // settings so any consumer-registered mappings are retained.
    // ReSharper disable once StaticMemberInGenericType
    private static readonly TypeAdapterConfig MappingConfig = BuildMappingConfig();

    private readonly ILogger<GenericGridDataFactory<TDataItem, TModel, TKey>> _logger;
    private readonly IGridDataSource<TDataItem> _dataSource;
    private readonly IEnumerable<IGridGlobalFilterProvider> _globalFilterProviders;

    public GenericGridDataFactory(ILogger<GenericGridDataFactory<TDataItem, TModel, TKey>> logger, IGridDataSource<TDataItem> dataSource, IEnumerable<IGridGlobalFilterProvider> globalFilterProviders)
    {
        _logger = logger;
        _dataSource = dataSource;
        _globalFilterProviders = globalFilterProviders;
    }

    public async Task<MaviGridCollection<TKey>?> BuildDataCollection(MaviGridDataQueryParameters queryParameters, Expression<Func<TDataItem, bool>>? additionalFilter = null)
    {
        if (string.IsNullOrWhiteSpace(queryParameters.OrderByColumnKey))
        {
            queryParameters.OrderByColumnKey = QueryConstants.ID;
        }

        Expression<Func<TDataItem, bool>>? filter = null;

        if (queryParameters.Filters != null && queryParameters.Filters.Any())
        {
            filter = ConstructFilter(queryParameters.Filters);
        }

        // Incorporate the consumer-supplied filter (e.g. permission or scope constraints) by
        // AND-combining it with the grid's own filter, so a user only ever sees data they are allowed
        // to and the same constraint applies to both the count and the page query.
        filter = CombineFilters(filter, additionalFilter);

        // Incorporate any globally registered, type-agnostic constraints (e.g. soft-delete or tenant
        // scoping) so they apply to every grid without per-page wiring.
        filter = CombineFilters(filter, BuildGlobalFilter());

        var totalItemCount = await _dataSource.Count(filter);

        var numberOfItemsToTake = ValidateNumberOfItemsToTake(queryParameters.NumberOfItemsPerPage);
        var totalPages = CalculateTotalPages(totalItemCount, numberOfItemsToTake);
        var requestedPageNumber = ValidateRequestedPage(totalPages, queryParameters.PageNumber);
        var skip = CalculateSkip(totalItemCount, requestedPageNumber, numberOfItemsToTake);


        IQueryable<TDataItem> ? resultingData = null;

        resultingData = await _dataSource.Get(queryParameters.SortOrder, queryParameters.OrderByColumnKey, skip, numberOfItemsToTake, filter, NavigationIncludePaths);

        if (resultingData == null)
        {
            return null;
        }

        var collection = ConstructCollection(resultingData);

        if (collection.Columns.Count == 0)
        {
            return collection;
        }

        collection.Take = numberOfItemsToTake;
        collection.TotalPages = totalPages;
        collection.TotalRows = totalItemCount;
        collection.CurrentPage = requestedPageNumber;
        collection.SortOrder = queryParameters.SortOrder;
        collection.SortColumnKey = queryParameters.OrderByColumnKey;

        collection.FirstRowIndex = totalItemCount == 0 ? 0 : (collection.CurrentPage - 1) * collection.Take + 1;
        collection.LastRowIndex = totalItemCount == 0 ? 0 : Math.Min(collection.CurrentPage * collection.Take, collection.TotalRows);

        var activeColumn = collection.Columns.FirstOrDefault(x => x.Key.Equals(queryParameters.OrderByColumnKey)) ?? collection.Columns.FirstOrDefault();

        if (activeColumn != null)
        {
            activeColumn.Active = true;
            activeColumn.SortOrder = queryParameters.SortOrder;
        }

        if (queryParameters.Filters == null)
        {
            return collection;
        }

        {
            foreach (var requestedFilter in queryParameters.Filters)
            {
                var correspondingColumn = collection.Columns.FirstOrDefault(x => x.Key == requestedFilter.Key);

                if (correspondingColumn == null)
                {
                    continue;
                }

                correspondingColumn.CurrentFilerValue = requestedFilter.Value;
                correspondingColumn.CurrentFilter = requestedFilter.GridFilterCriteria;
            }
        }

        return collection;
    }

    public MaviGridCollection<TKey> ConstructCollection(IQueryable<TDataItem> queryable)
    {
        var type = typeof(TModel);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var collection = new MaviGridCollection<TKey>();

        foreach (var dataItem in queryable)
        {
            var gridModelItem = dataItem.Adapt<TModel>(MappingConfig);
            gridModelItem.ConstructFrom(dataItem);

            var row = ConstructCollectionRow(gridModelItem, properties, collection.Columns);
            collection.Rows.Add(row);
        }

        return collection;
    }

    private static Expression<Func<TDataItem, bool>>? ConstructFilter(IEnumerable<GridFilterModel> filters)
    {
        Expression<Func<TDataItem, bool>>? expression = null;

        foreach (var item in filters)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || item.Value is null)
            {
                continue;
            }

            var filterExpression = QueryBuilder<TDataItem, TKey>.BuildFilterExpression(item.Key, item.GridFilterCriteria, item.Value);

            expression = expression == null ? filterExpression : QueryBuilder<TDataItem, TKey>.CombineExpressions(expression, filterExpression, MaviCombineExpressionCriteria.AndAlso);
        }

        return expression;
    }

    private Expression<Func<TDataItem, bool>>? BuildGlobalFilter()
    {
        Expression<Func<TDataItem, bool>>? combined = null;

        // AND-combine every registered provider's constraint so multiple cross-cutting concerns
        // (e.g. soft-delete and tenant scoping) can each be registered independently.
        foreach (var provider in _globalFilterProviders)
        {
            combined = CombineFilters(combined, provider.GetFilter<TDataItem>());
        }

        return combined;
    }

    private static Expression<Func<TDataItem, bool>>? CombineFilters(Expression<Func<TDataItem, bool>>? first, Expression<Func<TDataItem, bool>>? second)
    {
        if (first == null)
        {
            return second;
        }

        if (second == null)
        {
            return first;
        }

        return QueryBuilder<TDataItem, TKey>.CombineExpressions(first, second, MaviCombineExpressionCriteria.AndAlso);
    }

    /// <summary>
    /// Collects the names of the model's navigation properties (those marked with
    /// <see cref="GridNavPropertyAttribute"/>) so the data source can eager-load them. The property
    /// name is used verbatim as the include path, which relies on the navigation having the same name
    /// on the underlying data item as on the model.
    /// </summary>
    private static IReadOnlyCollection<string> BuildNavigationIncludePaths()
    {
        return typeof(TModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<GridNavPropertyAttribute>() != null)
            .Select(property => property.Name)
            .ToArray();
    }

    private static TypeAdapterConfig BuildMappingConfig()
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.Default.PreserveReference(true);
        return config;
    }

    private static int ValidateNumberOfItemsToTake(int numberOfItemsPerPage)
    {
        if (numberOfItemsPerPage != QueryConstants.TAKE_10
            && numberOfItemsPerPage != QueryConstants.TAKE_25
            && numberOfItemsPerPage != QueryConstants.TAKE_50 && numberOfItemsPerPage != QueryConstants.TAKE_100)
        {
            numberOfItemsPerPage = QueryConstants.TAKE_10;
        }

        return numberOfItemsPerPage;
    }

    private static int CalculateTotalPages(int totalItemCount, int numberOfItemsPerPage)
    {
        var totalPages = 1;

        if (totalItemCount <= numberOfItemsPerPage)
        {
            return totalPages;
        }

        if (totalItemCount % numberOfItemsPerPage > 0)
        {
            totalPages = totalItemCount / numberOfItemsPerPage + 1;
        }
        else
        {
            totalPages = totalItemCount / numberOfItemsPerPage;
        }

        return totalPages;
    }

    private static int ValidateRequestedPage(int totalPages, int requestedPage)
    {
        if (requestedPage > totalPages)
        {
            requestedPage = totalPages;
        }

        return requestedPage;
    }

    private static int CalculateSkip(int totalItemCount, int requestedPage, int numberOfItemsPerPage)
    {
        var skip = 0;

        if (totalItemCount > numberOfItemsPerPage && requestedPage > 1)
        {
            skip = numberOfItemsPerPage * (requestedPage - 1);
        }

        return skip;
    }

    private static MaviGridRow<TKey> ConstructCollectionRow(TModel gridModel, PropertyInfo[] properties, List<MaviGridColumn> columns)
    {
        var row = new MaviGridRow<TKey> { Key = gridModel.Key };

        foreach (var prop in properties)
        {
            // Handle properties marked with DataPropertyAttribute
            if (prop.GetCustomAttribute<GridDataPropertyAttribute>() != null)
            {
                var value = prop.GetValue(gridModel);

                var column = columns.FirstOrDefault(c => c.Key == prop.Name);

                if (column == null)
                {
                    column = new()
                    {
                        Key = prop.Name,
                        Title = DeterminePropertyTitle(prop),
                        GridPropertyType = GetPropertyType(prop.PropertyType),
                        Sequence = GetGridDataPropertySequence(prop),
                        MaviGridColumnBehavior = GetGridDataPropertyCellBehavior(prop)
                    };

                    ProcessColumn(column, prop);

                    columns.Add(column);
                }

                var cell = new MaviGridCell<TKey>
                {
                    Value = ConvertPropertyValueToCellValue(value),
                    Sequence = column.Sequence,
                    GridPropertyType = column.GridPropertyType,
                    ColumnKey = column.Key,
                    RowKey = row.Key,
                    GridColumnType = column.GridColumnType,
                    CellBehavior = GetGridDataPropertyCellBehavior(prop)
                };

                ProcessCell(column, cell, value);

                row.Cells.Add(cell);
            }
            // Handle nested properties marked with NestedDataPropertyAttribute
            else if (prop.GetCustomAttribute<GridNavPropertyAttribute>() != null)
            {
                var nestedInstance = prop.GetValue(gridModel);
                if (nestedInstance == null)
                {
                    continue;
                }

                var nestedProperties = prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var nestedProp in nestedProperties)
                {
                    if (nestedProp.GetCustomAttribute<GridNestedPropertyAttribute>() != null)
                    {
                        var nestedPropertyKey = $"{prop.Name}.{nestedProp.Name}";

                        var nestedPropertyColumn = columns.FirstOrDefault(c => c.Key == nestedPropertyKey);

                        if (nestedPropertyColumn == null)
                        {
                            nestedPropertyColumn = new()
                            {
                                Key = nestedPropertyKey,
                                Title = DetermineNestedPropertyTitle(nestedProp),
                                GridPropertyType = GetPropertyType(nestedProp.PropertyType),
                                Sequence = GridNavPropertySequence(prop)
                            };

                            ProcessColumn(nestedPropertyColumn, nestedProp);

                            columns.Add(nestedPropertyColumn);
                        }

                        var nestedValue = nestedProp.GetValue(nestedInstance);

                        var nestedCell = new MaviGridCell<TKey>
                        {
                            Value = ConvertPropertyValueToCellValue(nestedValue),
                            Sequence = nestedPropertyColumn.Sequence,
                            GridPropertyType = nestedPropertyColumn.GridPropertyType,
                            ColumnKey = nestedPropertyColumn.Key,
                            RowKey = row.Key,
                            GridColumnType = nestedPropertyColumn.GridColumnType,
                            CellBehavior = GridNavPropertyType(prop)
                        };

                        ProcessCell(nestedPropertyColumn, nestedCell, nestedValue);

                        row.Cells.Add(nestedCell);
                    }
                }
            }
        }

        row.ContextActions = gridModel.GetContextActions().ToList();

        return row;
    }

    private static (string positive, string negative) GetUserFriendlyBooleanDisplay(PropertyInfo property)
    {
        // Retrieve the [Display] attribute
        var booleanDisplayValuesAttribute = property.GetCustomAttribute<GridBooleanDisplayValuesAttribute>();

        if (booleanDisplayValuesAttribute != null && !string.IsNullOrWhiteSpace(booleanDisplayValuesAttribute.Positive) && !string.IsNullOrWhiteSpace(booleanDisplayValuesAttribute.Negative))
        {
            return (booleanDisplayValuesAttribute.Positive, booleanDisplayValuesAttribute.Negative);
        }

        return (QueryConstants.TRUE, QueryConstants.FALSE);
    }

    private static string DeterminePropertyTitle(PropertyInfo property)
    {
        // Retrieve the [Display] attribute
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();

        if (displayAttribute != null && !string.IsNullOrWhiteSpace(displayAttribute.Name))
        {
            return displayAttribute.Name;
        }

        return property.Name;
    }

    private static string DetermineNestedPropertyTitle(PropertyInfo property)
    {
        // Retrieve the [Display] attribute
        var gridNestedPropertyAttribute = property.GetCustomAttribute<GridNestedPropertyAttribute>();

        if (gridNestedPropertyAttribute != null && !string.IsNullOrWhiteSpace(gridNestedPropertyAttribute.Title))
        {
            return gridNestedPropertyAttribute.Title;
        }

        return property.Name;
    }

    private static int GetGridDataPropertySequence(PropertyInfo property)
    {
        var sequence = 1;

        // Retrieve the [Display] attribute
        var gridDataPropertyAttribute = property.GetCustomAttribute<GridDataPropertyAttribute>();

        if (gridDataPropertyAttribute != null)
        {
            sequence = gridDataPropertyAttribute.Sequence;
        }

        if (sequence <= 0)
        {
            sequence = 1;
        }

        return sequence;
    }

    private static int GridNavPropertySequence(PropertyInfo property)
    {
        var sequence = 1;

        // Retrieve the [Display] attribute
        var gridNavPropertyAttribute = property.GetCustomAttribute<GridNavPropertyAttribute>();

        if (gridNavPropertyAttribute != null)
        {
            sequence = gridNavPropertyAttribute.Sequence;
        }

        if (sequence <= 0)
        {
            sequence = 1;
        }

        return sequence;
    }

    private static MaviGridPropertyType GetPropertyType(Type type, bool isNavigation = false)
    {
        // Collapse Nullable<T> to T so that, for example, int and int? share a mapping.
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(string))
        {
            return isNavigation ? MaviGridPropertyType.NavString : MaviGridPropertyType.String;
        }

        if (t == typeof(char))
        {
            return isNavigation ? MaviGridPropertyType.NavChar : MaviGridPropertyType.Char;
        }

        if (t == typeof(bool))
        {
            return isNavigation ? MaviGridPropertyType.NavBool : MaviGridPropertyType.Bool;
        }

        // All integral types share the Int category.
        if (t == typeof(byte) || t == typeof(sbyte)
            || t == typeof(short) || t == typeof(ushort)
            || t == typeof(int) || t == typeof(uint)
            || t == typeof(long) || t == typeof(ulong))
        {
            return isNavigation ? MaviGridPropertyType.NavInt : MaviGridPropertyType.Int;
        }

        // All real/floating-point types share the Decimal category.
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
        {
            return isNavigation ? MaviGridPropertyType.NavDecimal : MaviGridPropertyType.Decimal;
        }

        if (t.IsEnum)
        {
            return isNavigation ? MaviGridPropertyType.NavEnum : MaviGridPropertyType.Enum;
        }

        // All point-in-time types share the DateTime category; formatting is resolved per runtime type.
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly) || t == typeof(TimeOnly))
        {
            return isNavigation ? MaviGridPropertyType.NavDateTime : MaviGridPropertyType.DateTime;
        }

        if (t == typeof(TimeSpan))
        {
            return isNavigation ? MaviGridPropertyType.NavTimeSpan : MaviGridPropertyType.TimeSpan;
        }

        if (t == typeof(Guid))
        {
            return isNavigation ? MaviGridPropertyType.NavGuid : MaviGridPropertyType.Guid;
        }

        return MaviGridPropertyType.Undefined;
    }

    private static void ProcessColumn(MaviGridColumn column, PropertyInfo property)
    {
        switch (column.GridPropertyType)
        {
            case MaviGridPropertyType.Bool or MaviGridPropertyType.NavBool:
                var (positive, negative) = GetUserFriendlyBooleanDisplay(property);

                column.BoolPositiveDisplay = positive;
                column.BoolNegativeDisplay = negative;
                break;
            case MaviGridPropertyType.Enum or MaviGridPropertyType.NavEnum:
                {
                    var enumType = property.PropertyType;
                    column.PossibleFilterValues = Enum.GetValues(enumType).Cast<Enum>().ToDictionary(enumValue => enumValue.ToString(), enumValue => enumValue.GetEnumDisplayName());
                    break;
                }
        }
    }

    private static void ProcessCell(MaviGridColumn column, MaviGridCell<TKey> cell, object? value)
    {
        switch (column.GridPropertyType)
        {
            // set value for boolean cell to user understandable instead of isTrue or isFalse
            case MaviGridPropertyType.Bool:
            case MaviGridPropertyType.NavBool:
                {
                    if (value == null)
                    {
                        cell.Value = column.BoolNegativeDisplay;
                    }
                    else
                    {
                        var cellValue = value.ToString();

                        if (string.IsNullOrWhiteSpace(cellValue))
                        {
                            cell.Value = column.BoolNegativeDisplay;
                        }
                        else
                            cell.Value = cellValue switch
                            {
                                QueryConstants.TRUE => column.BoolPositiveDisplay,
                                QueryConstants.FALSE => column.BoolNegativeDisplay,
                                _ => value?.ToString() ?? string.Empty
                            };
                    }

                    break;
                }
            case MaviGridPropertyType.Enum or MaviGridPropertyType.NavEnum when value == null:
                cell.Value = string.Empty;
                break;
            case MaviGridPropertyType.Enum or MaviGridPropertyType.NavEnum:
                {
                    var enumValue = (Enum)value;

                    cell.Value = enumValue.GetEnumDisplayName();
                    break;
                }
            default:
                cell.Value = ConvertPropertyValueToCellValue(value);
                break;
        }
    }

    private static string ConvertPropertyValueToCellValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            // IFormattable covers the numeric types so they render with a stable, culture-invariant form.
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static MaviGridColumnCellBehavior GetGridDataPropertyCellBehavior(PropertyInfo property)
    {
        // Retrieve the [Display] attribute
        var gridDataPropertyAttribute = property.GetCustomAttribute<GridDataPropertyAttribute>();

        return gridDataPropertyAttribute?.ColumnCellBehavior ?? MaviGridColumnCellBehavior.Default;
    }

    private static MaviGridColumnCellBehavior GridNavPropertyType(PropertyInfo property)
    {
        // Retrieve the [Display] attribute
        var gridNavPropertyAttribute = property.GetCustomAttribute<GridNavPropertyAttribute>();

        return gridNavPropertyAttribute?.CellType ?? MaviGridColumnCellBehavior.Default;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or
    /// resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>

    #region dispose
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources here
            await _dataSource.DisposeAsync();
        }

        // Free unmanaged resources here, if any

        _disposed = true;
    }

    #endregion
}
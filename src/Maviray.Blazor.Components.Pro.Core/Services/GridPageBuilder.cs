using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>
/// Builds grid pages from data items an app loaded itself (for example through its own service, behind its own
/// authorization), with the columns, cells, formatting and denied state that <see cref="GridDataFactory{TDataItem, TModel, TKey}"/>
/// produces. Use it in a <c>MaviGrid</c> <c>FetchData</c> delegate when the factory can't serve the page.
/// </summary>
/// <remarks>
/// <typeparamref name="TDataItem"/> can be a DTO. Each item is mapped to a <typeparamref name="TModel"/> by matching
/// member names, then <see cref="IGridCollectionModel{TDataItem, TKey}.ConstructFrom"/> runs. A column is sortable and
/// filterable when <typeparamref name="TDataItem"/> has a member with its name (or the path map gives one): those are
/// the column keys the app's query code must handle.
/// </remarks>
public static class GridPageBuilder<TDataItem, TModel, TKey>
    where TDataItem : class
    where TModel : class, IGridCollectionModel<TDataItem, TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>The grid's columns in display order, as every built page carries them.</summary>
    /// <param name="map">The grid's path map, if it has one.</param>
    /// <param name="hiddenColumns">Keys of columns the user may not see, left out.</param>
    public static IReadOnlyList<MaviGridColumn> Columns(GridPropertyPathMap<TDataItem, TModel>? map = null, IReadOnlyCollection<string>? hiddenColumns = null) =>
        GridModelMetadata<TDataItem, TModel>.Instance.ColumnsFor(map).Without(hiddenColumns).Columns;

    /// <summary>Builds the page from the items loaded for <paramref name="query"/>.</summary>
    /// <param name="page">The items and the paging totals.</param>
    /// <param name="query">The query the items were loaded for; its sort is reported when the column is sortable.</param>
    /// <param name="options">Only <see cref="GridFetchOptions{TDataItem, TModel}.PropertyPathMap"/> and
    /// <see cref="GridFetchOptions{TDataItem, TModel}.CellThemes"/> apply. Reuse one instance per grid.</param>
    public static MaviGridCollection<TKey> Build(GridLoadedPage<TDataItem> page, MaviGridQuery query, GridFetchOptions<TDataItem, TModel>? options = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(query);

        var metadata = GridModelMetadata<TDataItem, TModel>.Instance;
        var columns = metadata.ColumnsFor(options?.PropertyPathMap).Without(page.HiddenColumns);
        var pageSize = Math.Max(page.PageSize, 1);

        return new MaviGridCollection<TKey>
        {
            Columns = columns.Columns,
            Rows = BuildRows(page.Items, metadata, columns, options?.CellThemes),
            PageNumber = page.PageNumber,
            PageSize = pageSize,
            TotalRows = page.TotalRows,
            TotalPages = (int)((page.TotalRows + (long)pageSize - 1) / pageSize),
            SortColumnKey = columns.TryGetPath(query.SortColumnKey, out _) ? query.SortColumnKey : null,
            SortOrder = query.SortOrder,
            NextPageCursor = page.NextPageCursor,
            PreviousPageCursor = page.PreviousPageCursor
        };
    }

    /// <summary>A denied page: the grid's columns, nothing sortable or filterable, and no rows.</summary>
    /// <param name="message">Shown to the user instead of the grid's access-denied title.</param>
    public static MaviGridCollection<TKey> Denied(string? message = null) => new()
    {
        Columns = GridModelMetadata<TDataItem, TModel>.Instance.DeniedColumns,
        AccessDenied = true,
        AccessDeniedMessage = message
    };

    private static MaviGridRow<TKey>[] BuildRows(
        IReadOnlyList<TDataItem> items,
        GridModelMetadata<TDataItem, TModel> metadata,
        GridColumnSet columns,
        IReadOnlyDictionary<string, Func<object?, ThemeColorScheme>>? cellThemes)
    {
        var descriptors = metadata.Descriptors;
        var descriptorIndexes = columns.DescriptorIndexes;
        var rows = new MaviGridRow<TKey>[items.Count];

        for (var r = 0; r < items.Count; r++)
        {
            var item = items[r];
            var model = metadata.MapToModel(item);
            model.ConstructFrom(item);

            // One cell per column of the page: a hidden column's value is never formatted.
            var cells = new MaviGridCell[descriptorIndexes.Count];

            for (var c = 0; c < cells.Length; c++)
            {
                var descriptor = descriptors[descriptorIndexes[c]];

                if (!descriptor.TryGetValue(model, out var value))
                {
                    cells[c] = MaviGridCell.Empty;
                    continue;
                }

                var theme = descriptor.GraphicDisplay && cellThemes is not null && cellThemes.TryGetValue(descriptor.Key, out var resolve)
                    ? resolve(value)
                    : default;

                cells[c] = new MaviGridCell(descriptor.Format(value), theme);
            }

            var actions = model.GetContextActions();

            rows[r] = new MaviGridRow<TKey>
            {
                Key = model.Key,
                Cells = cells,
                ContextActions = actions switch
                {
                    null => [],
                    IReadOnlyList<IMaviRowContextMenuItem<TKey>> list => list,
                    _ => actions.ToArray()
                }
            };
        }

        return rows;
    }
}

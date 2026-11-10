using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Tests.Fixtures;

/// <summary>Pages shaped like the factory's output, with columns Id (int), Name (string) and Note (not sortable or filterable).</summary>
public static class GridPages
{
    public static readonly IReadOnlyList<MaviGridColumn> Columns =
    [
        new() { Key = "Id", Title = "Id", Index = 0, Sequence = 1, PropertyType = MaviGridPropertyType.Int, Sortable = true, Filterable = true },
        new() { Key = "Name", Title = "Name", Index = 1, Sequence = 2, PropertyType = MaviGridPropertyType.String, Sortable = true, Filterable = true },
        new() { Key = "Note", Title = "Note", Index = 2, Sequence = 3, PropertyType = MaviGridPropertyType.String }
    ];

    public static MaviGridCollection<int> Page(MaviGridQuery query, params int[] keys) => PageOf(query, keys.Length, keys);

    public static MaviGridCollection<int> PageOf(MaviGridQuery query, int totalRows, params int[] keys) => new()
    {
        Columns = Columns,
        Rows = keys.Select(key => new MaviGridRow<int>
        {
            Key = key,
            Cells = [new MaviGridCell(key.ToString()), new MaviGridCell($"Name {key}"), new MaviGridCell($"Note {key}")]
        }).ToArray(),
        PageNumber = query.PageNumber,
        PageSize = query.PageSize,
        TotalRows = totalRows,
        TotalPages = (totalRows + query.PageSize - 1) / query.PageSize,
        SortColumnKey = query.SortColumnKey,
        SortOrder = query.SortOrder
    };

    public static MaviGridCollection<int> Denied(string? message = null) => new()
    {
        Columns = Columns.Select(c => new MaviGridColumn { Key = c.Key, Title = c.Title, Index = c.Index, Sequence = c.Sequence, PropertyType = c.PropertyType }).ToArray(),
        AccessDenied = true,
        AccessDeniedMessage = message
    };
}

/// <summary>A FetchData delegate that records every query it receives.</summary>
public sealed class RecordingFetch(Func<MaviGridQuery, int, MaviGridCollection<int>> respond)
{
    public RecordingFetch(params int[] keys) : this((query, _) => GridPages.Page(query, keys))
    {
    }

    public List<MaviGridQuery> Queries { get; } = [];
    public List<CancellationToken> Tokens { get; } = [];

    public Task<MaviGridCollection<int>> Invoke(MaviGridQuery query, CancellationToken cancellationToken)
    {
        Queries.Add(query);
        Tokens.Add(cancellationToken);
        return Task.FromResult(respond(query, Queries.Count));
    }
}

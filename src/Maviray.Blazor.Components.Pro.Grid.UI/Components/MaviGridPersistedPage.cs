using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Menu;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Components;

/// <summary>
/// A grid page, and the query that loaded it, in a shape <c>PersistentComponentState</c> can store as JSON: row actions
/// become <see cref="MaviRowContextMenuItem{TKey}"/> with the same values.
/// </summary>
internal sealed class MaviGridPersistedPage<TKey> where TKey : IEquatable<TKey>
{
    public MaviGridQuery Query { get; set; } = new();
    public List<MaviGridColumn> Columns { get; set; } = [];
    public List<PersistedRow> Rows { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public string? SortColumnKey { get; set; }
    public SortOrder SortOrder { get; set; }
    public bool AccessDenied { get; set; }
    public string? AccessDeniedMessage { get; set; }
    public string? NextPageCursor { get; set; }
    public string? PreviousPageCursor { get; set; }

    public static MaviGridPersistedPage<TKey> From(MaviGridCollection<TKey> page, MaviGridQuery query) => new()
    {
        Query = query,
        Columns = [.. page.Columns],
        Rows = [.. page.Rows.Select(r => new PersistedRow
        {
            Key = r.Key,
            Cells = [.. r.Cells],
            Actions = [.. r.ContextActions.Select(a => new MaviRowContextMenuItem<TKey>
            {
                Key = a.Key,
                Title = a.Title,
                Icon = a.Icon,
                Disabled = a.Disabled,
                ElementVariant = a.ElementVariant,
                ThemeColorScheme = a.ThemeColorScheme,
                ElementSize = a.ElementSize
            })]
        })],
        PageNumber = page.PageNumber,
        PageSize = page.PageSize,
        TotalRows = page.TotalRows,
        TotalPages = page.TotalPages,
        SortColumnKey = page.SortColumnKey,
        SortOrder = page.SortOrder,
        AccessDenied = page.AccessDenied,
        AccessDeniedMessage = page.AccessDeniedMessage,
        NextPageCursor = page.NextPageCursor,
        PreviousPageCursor = page.PreviousPageCursor
    };

    /// <summary>Whether the page answers <paramref name="query"/>: the same page, size, sort and filters.</summary>
    public bool Answers(MaviGridQuery query) =>
        Query.PageNumber == query.PageNumber
        && Query.PageSize == query.PageSize
        && Query.SortColumnKey == query.SortColumnKey
        && Query.SortOrder == query.SortOrder
        && Query.Filters.SequenceEqual(query.Filters);

    public MaviGridCollection<TKey> ToCollection() => new()
    {
        Columns = Columns,
        Rows = [.. Rows.Select(r => new MaviGridRow<TKey> { Key = r.Key, Cells = r.Cells, ContextActions = r.Actions })],
        PageNumber = PageNumber,
        PageSize = PageSize,
        TotalRows = TotalRows,
        TotalPages = TotalPages,
        SortColumnKey = SortColumnKey,
        SortOrder = SortOrder,
        AccessDenied = AccessDenied,
        AccessDeniedMessage = AccessDeniedMessage,
        NextPageCursor = NextPageCursor,
        PreviousPageCursor = PreviousPageCursor
    };

    public sealed class PersistedRow
    {
        public TKey Key { get; set; } = default!;
        public List<MaviGridCell> Cells { get; set; } = [];
        public List<MaviRowContextMenuItem<TKey>> Actions { get; set; } = [];
    }
}

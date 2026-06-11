namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

public class MaviGridCollection<TKey> where TKey : IEquatable<TKey>
{
    public int Take { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; } = 1;

    public List<MaviGridColumn> Columns { get; set; } = [];
    public List<MaviGridRow<TKey>> Rows { get; set; } = [];

    public string GetActiveColumnGuid()
    {
        if (Columns.Count == 0)
        {
            return string.Empty;
        }

        var activeColumn = Columns.FirstOrDefault(x => x.Active);

        return activeColumn != null ? activeColumn.Guid : Columns.First().Guid;
    }

    public int GetTopSequence()
    {
        return Columns.Count == 0 ? 1 : Columns.Select(column => column.Sequence).Prepend(1).Max();
    }
}
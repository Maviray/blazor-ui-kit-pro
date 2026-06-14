using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;

public class MaviGridDataQueryParameters
{
    public SortOrder SortOrder { get; set; } = SortOrder.Ascending;
    public int NumberOfItemsPerPage { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
    public string? OrderByColumnKey { get; set; }
    public List<GridFilterModel>? Filters { get; set; }

    public void AddFilter(string columnKey, string filterValue, MaviGridFilterCriteria gridFilterCriteria)
    {
        Filters ??= [];

        var existingFilter = Filters.FirstOrDefault(x => x.Key == columnKey);

        if (existingFilter == null)
        {
            existingFilter = new() { Key = columnKey };
            Filters.Add(existingFilter);
        }

        existingFilter.Value = filterValue;
        existingFilter.GridFilterCriteria = gridFilterCriteria;
    }

    public void RemoveFilter(string columnKey)
    {
        Filters?.RemoveAll(x => x.Key == columnKey);
    }

    public void ClearFilters()
    {
        Filters?.Clear();
    }
}
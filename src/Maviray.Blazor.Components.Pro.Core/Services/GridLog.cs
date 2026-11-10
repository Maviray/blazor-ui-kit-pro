using Microsoft.Extensions.Logging;

namespace Maviray.Blazor.Components.Pro.Core.Services;

internal static partial class GridLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Grid fetch of {DataItemType} denied (access tag: {AccessTag}).")]
    public static partial void FetchDenied(ILogger logger, string dataItemType, string? accessTag);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Grid page {PageNumber} of {DataItemType} loaded: {RowCount} of {TotalRows} rows.")]
    public static partial void PageLoaded(ILogger logger, string dataItemType, int pageNumber, int rowCount, int totalRows);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Grid filter on '{ColumnKey}' ignored for {DataItemType}: not a filterable column.")]
    public static partial void FilterIgnored(ILogger logger, string columnKey, string dataItemType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Grid sort on '{ColumnKey}' ignored for {DataItemType}: not a sortable column.")]
    public static partial void SortIgnored(ILogger logger, string columnKey, string dataItemType);
}

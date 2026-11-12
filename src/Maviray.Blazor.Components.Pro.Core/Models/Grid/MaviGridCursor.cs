namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// For keyset paging: load the page just after (<see cref="MaviGridCursorDirection.Next"/>) or just before
/// (<see cref="MaviGridCursorDirection.Previous"/>) the position in <see cref="Value"/>, instead of skipping rows.
/// </summary>
/// <param name="Value">A cursor the fetch code returned in <see cref="MaviGridCollection{TKey}.NextPageCursor"/> or
/// <see cref="MaviGridCollection{TKey}.PreviousPageCursor"/>; the grid only echoes it.</param>
public sealed record MaviGridCursor(string Value, MaviGridCursorDirection Direction);

public enum MaviGridCursorDirection
{
    /// <summary>The rows that follow the position, in the query's order.</summary>
    Next,

    /// <summary>The rows that precede the position, in the query's order.</summary>
    Previous
}

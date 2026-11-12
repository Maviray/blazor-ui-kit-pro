namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>One page loaded by keyset order, with the cursors of its first and last rows.</summary>
/// <param name="Items">The rows of the page, in the order's direction.</param>
/// <param name="FirstRowCursor">Seek <see cref="MaviGridCursorDirection.Previous"/> from here for the page before;
/// <see langword="null"/> when the page is empty or can't be sought from.</param>
/// <param name="LastRowCursor">Seek <see cref="MaviGridCursorDirection.Next"/> from here for the page after.</param>
public sealed record GridKeysetPage<T>(IReadOnlyList<T> Items, string? FirstRowCursor, string? LastRowCursor);

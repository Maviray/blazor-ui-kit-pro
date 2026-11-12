using Maviray.Blazor.Components.Core.Enums;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// A formatted cell value. <see cref="ThemeColorScheme"/> is set for graphic columns from the grid's cell themes.
/// </summary>
public readonly record struct MaviGridCell(string Text, ThemeColorScheme ThemeColorScheme = default)
{
    public static MaviGridCell Empty { get; } = new(string.Empty);
}

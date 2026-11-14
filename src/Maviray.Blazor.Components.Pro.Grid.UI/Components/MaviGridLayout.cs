using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Material.Constants;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using System.Globalization;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Components;

/// <summary>
/// The visible columns and every CSS string the grid renders, computed once per change of columns, column
/// visibility or appearance settings. Rows compare the layout by reference to skip re-rendering.
/// </summary>
internal sealed class MaviGridLayout
{
    private readonly string[][] _cellCss;
    private readonly string[] _selectCss;
    private readonly string[] _actionsCss;

    private MaviGridLayout(IReadOnlyList<MaviGridColumn> columns, MaviGridParameters parameters, bool accessDenied)
    {
        Columns = columns;
        ShowSelect = parameters.DisplaySelectColumn && !accessDenied;
        ShowFilters = parameters.EnableFilters && !accessDenied;
        RowActionsAsIcons = parameters.TableRowContextMenuDisplayStyle == TableRowContextMenuDisplayStyle.Icons;
        MaxCellChars = parameters.MaxCellCharsToDisplay;

        var spacing = parameters.Condensed ? "px-2 py-1.5" : "px-4 py-3";
        var textSize = parameters.Condensed ? Tailwind.Theme.Typography.BODY_SMALL : Tailwind.Theme.Typography.BODY_MEDIUM;
        string Background(int parity) => parameters.Striped && parity == 1 ? "bg-zinc-50" : "bg-white";
        string Cell(int parity, string extra) => $"{Background(parity)} {spacing} text-zinc-800 truncate flex items-center group-hover:bg-zinc-100 {extra}";

        _cellCss =
        [
            columns.Select(c => Cell(0, CellExtra(c))).ToArray(),
            columns.Select(c => Cell(1, CellExtra(c))).ToArray()
        ];
        _selectCss = [Cell(0, "justify-center"), Cell(1, "justify-center")];
        _actionsCss = [Cell(0, "justify-end"), Cell(1, "justify-end")];

        GridStyle = $"grid-template-columns: {Tracks(columns, parameters.StretchColumns, ShowSelect)}";
        HeaderCellCss = $"{spacing} truncate flex items-center justify-start";
        SelectHeaderCss = $"{spacing} truncate flex items-center justify-center";
        MenuCellCss = $"{spacing} truncate flex items-center justify-end";
        FooterCss = $"bg-white {spacing} flex items-center justify-between gap-4 w-full min-w-80";
        PagerTextCss = $"{textSize} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_EIGHT_TEXT}";
        PagerInputCss = $"{textSize} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_NINE_TEXT} {Tailwind.Theme.Colors.Background.THEME_DEFAULT_ONE_BG} " +
                        $"{Tailwind.Theme.Colors.Border.THEME_DEFAULT_THREE_BORDER} {Tailwind.Theme.Colors.Ring.Focus.THEME_PRIMARY_SIX_RING_FOCUS} " +
                        "border rounded-md px-2 py-1 cursor-pointer focus:outline-none focus:ring-2 transition-shadow w-[70px]";
    }

    public static MaviGridLayout Empty { get; } = new([], new MaviGridParameters(), accessDenied: false);

    public IReadOnlyList<MaviGridColumn> Columns { get; }

    public bool ShowSelect { get; }

    public bool ShowFilters { get; }

    public bool RowActionsAsIcons { get; }

    public int MaxCellChars { get; }

    public string GridStyle { get; }

    public string HeaderCellCss { get; }

    /// <summary>Centred over the row checkboxes. Its own string: a second justify class on a header cell loses to the
    /// one later in the stylesheet.</summary>
    public string SelectHeaderCss { get; }

    public string MenuCellCss { get; }

    public string FooterCss { get; }

    public string PagerTextCss { get; }

    public string PagerInputCss { get; }

    public const string HeaderButtonCss = "flex items-center gap-2 select-none transition-colors cursor-pointer bg-transparent text-left";

    public const string FilterCellCss = "px-4 py-2 bg-zinc-50";

    public const string StateCss = "w-full min-h-40 flex items-center justify-center gap-4 col-span-full p-4";

    public static readonly string HeaderTextCss = $"{Tailwind.Theme.Typography.LABEL_LARGE} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_NINE_TEXT}";

    public static readonly string HeaderButtonHoverCss = Tailwind.Theme.Colors.Text.Hover.THEME_DEFAULT_TEN_TEXT_HOVER;

    public static readonly string StateTextCss = $"{Tailwind.Theme.Typography.BODY_LARGE} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_SEVEN_TEXT}";

    public static readonly string FilterInputCss =
        $"{Tailwind.Theme.Typography.BODY_SMALL} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_NINE_TEXT} {Tailwind.Theme.Colors.Border.THEME_DEFAULT_THREE_BORDER} " +
        $"{Tailwind.Theme.Colors.Ring.Focus.THEME_PRIMARY_SIX_RING_FOCUS} bg-white border px-2 py-1 w-full focus:outline-none focus:ring-2 placeholder:text-(--theme-default-five)";

    public static readonly string DropdownCss =
        $"{Tailwind.Theme.Colors.Background.THEME_DEFAULT_ONE_BG} {Tailwind.Theme.Colors.Border.THEME_DEFAULT_THREE_BORDER} w-64 border rounded-lg shadow-lg z-[9999] p-2";

    public static readonly string MenuItemCss =
        $"{Tailwind.Theme.Typography.BODY_MEDIUM} {Tailwind.Theme.Colors.Text.THEME_DEFAULT_NINE_TEXT} {Tailwind.Theme.Colors.Background.Hover.THEME_DEFAULT_TWO_BG_HOVER} " +
        "block w-full px-4 py-2.5 text-left rounded-md transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed";

    public string CellCss(int rowIndex, int visibleColumn) => _cellCss[rowIndex & 1][visibleColumn];

    public string SelectCss(int rowIndex) => _selectCss[rowIndex & 1];

    public string ActionsCss(int rowIndex) => _actionsCss[rowIndex & 1];

    public static MaviGridLayout Create(IReadOnlyList<MaviGridColumn> columns, IReadOnlySet<string> hidden, MaviGridParameters parameters, bool accessDenied)
    {
        var visible = columns.Where(c => !hidden.Contains(c.Key)).OrderBy(c => c.Sequence).ThenBy(c => c.Index).ToArray();
        return new MaviGridLayout(visible, parameters, accessDenied);
    }

    /// <summary>
    /// One track per rendered column: the visible data columns, the select column, the actions column. Stretching
    /// columns share the free width by weight; when none is visible, the actions column takes it.
    /// </summary>
    private static string Tracks(IReadOnlyList<MaviGridColumn> visible, IReadOnlyDictionary<string, int>? overrides, bool showSelect)
    {
        const string fixedTrack = "minmax(min-content, 200px)";
        var tracks = new List<string>(visible.Count + 2);
        var anyStretch = false;

        foreach (var column in visible)
        {
            var weight = overrides is null ? column.StretchWeight : overrides.GetValueOrDefault(column.Key);
            anyStretch |= weight > 0;
            tracks.Add(weight > 0 ? $"minmax(min-content, {weight.ToString(CultureInfo.InvariantCulture)}fr)" : fixedTrack);
        }

        if (showSelect)
        {
            tracks.Add(fixedTrack);
        }

        tracks.Add(anyStretch ? "min-content" : "minmax(min-content, 1fr)");
        return string.Join(' ', tracks);
    }

    private static string CellExtra(MaviGridColumn column) =>
        column.CellBehavior == MaviGridColumnCellBehavior.Navigational ? "justify-start cursor-pointer font-semibold" : "justify-start";
}

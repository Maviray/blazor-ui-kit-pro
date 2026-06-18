using Maviray.Blazor.Components.Core.Enums;

namespace Maviray.Blazor.Components.Pro.Grid.UI.Components;

internal class MaviGridState
{
    // CSS caching fields
    internal string? CellSpacing;
    internal string? ContextTextSize;
    internal IconSize? IconSize;
    internal string? CountPerPageDropdownCss;
    internal string? ContextMenuCellCss;
    internal string? FooterRowCss;
    internal string? SearchInputCss;
    internal string? ContextMenuDropDownCss;
    internal string MmainMenuDropdownPositionStyle = string.Empty;
    internal string ActiveRowMenuDropdownPositionStyle = string.Empty;
    internal TableRowContextMenuDisplayStyle TableRowContextMenuDisplayStyle;
    internal string? ContextMenuId;
    internal bool BackdropInitialState = true;
    internal bool ContextMenuVisible;
    internal bool NeedsRender;
    internal int GoToPage;
}
# Data Tables

Namespace `Maviray.Blazor.Components.Material.Components.Tables`. Data models live
in `Maviray.Blazor.Components.Core.Models.Tables` and the column attribute in
`Maviray.Blazor.Components.Core.Attributes`.

Two components render an in-memory, client-side data grid with sorting, optional
per-column filtering, pagination, a toolbar ("main") context menu, and per-row
context menus. They share **identical API** (both inherit
`MaviInMemoryTableBase`) and differ only in markup:

- **`MaviInMemoryTable`** — semantic HTML `<table>`.
- **`MaviCssGridTable`** — CSS-grid layout (`grid-template-columns`), better for
  custom cell layouts.

Both show a loading backdrop+spinner while data is (re)fetched.

## Parameters (both components)

| Parameter | Type | Purpose |
|-----------|------|---------|
| `FetchData` | `Func<Task<TableDataCollection>>?` | Async data source. Called on init and on `Refresh()`. Return a built `TableDataCollection` (see below). |
| `ConstructContextMenu` | `Func<Task<IEnumerable<MaviTableContextMenuItem>>>?` | Builds the toolbar ("main") action menu (the meatballs button in the header). |
| `Parameters` | `TableParameters?` | Behavior/labels/appearance config (below). Defaults to `new()`. |
| `NoDataContent` | `RenderFragment?` | Custom empty-state content. |
| `OnRowClick` | `EventCallback<TableClickData>` | Fires when a cell/row is clicked; payload has `.Row` and `.Column`. |
| `OnMainContextMenuClick` | `EventCallback<MaviTableContextMenuItem>` | Fires when a toolbar menu action is chosen. |
| `OnRowContextMenuClick` | `EventCallback<MaviTableRowContextMenuItem>` | Fires when a per-row action is chosen. |
| `Class`, `Style`, `Id` | | As base. |

## Method

- `Task Refresh()` — re-invoke `FetchData` + `ConstructContextMenu` and re-render
  (shows the loading backdrop). Call via `@ref` after mutating the source.

## TableParameters

| Property | Default | Purpose |
|----------|---------|---------|
| `EnableFilters` | `false` | Show a per-column filter row (debounced ~300ms, contains-match). |
| `TableRowContextMenuDisplayStyle` | `DropDown` | `DropDown` (meatballs → menu) or `Icons` (inline action icons). |
| `DisplaySelectColumn` | `false` | Reserve a selection column. |
| `PaginationHorizontalAlignment` | `Right` | `Left/Center/Right`. |
| `Striped` | `false` | Zebra rows. |
| `Condensed` | `false` | Tighter rows. |
| `MaxCellCharsToDisplay` | 0 | Truncate cell text with "…" (0 = no limit). |
| `SortOrder` | `Ascending` | Initial sort direction. |
| `BackdropOpacity` / `ZIndex` | `Lighten` / `Forty` | Loading overlay. |
| `ContentMenuTitle` | `"Actions"` | Toolbar menu button title. |
| `NoDataAvailableTitle` | `"No data available to display"` | Empty state. |
| `RowsPerPageText`, `SelectNumberOfRowsToDisplayText`, `GoToPreviousPageText`, `GoToNextPageText`, `FilterPlaceholderText` | (English defaults) | Pagination/filter localization strings. |

## The data model: `TableDataCollection`

`FetchData` returns a `TableDataCollection` — a set of `Columns`
(`MaviTableColumn`) and `Rows` (`MaviTableRow`), plus built-in paging/sorting/
filtering state (`CurrentPage`, `PageSize` (default 10), `TotalPages`,
`SortColumnKey`, `SortOrder`, `GlobalSearchText`, column filters). The table
manages this state internally as the user interacts:

- **Filtering returns to page 1.** Setting `GlobalSearchText` (case-insensitive
  contains-match over cell text) or a column filter refilters the rows and goes
  back to page 1.
- **Sorting** compares values of the same type by their natural order
  (`IComparable`, so `long` and `decimal` values sort numerically, dates
  chronologically); text, and values of mixed types, compare by display text,
  ignoring case and accents in the current culture. Missing cells and `null`
  values sort after all values when ascending; descending reverses the whole
  order, so they come first.
- **New parameters re-render the table.** Passing a new `Parameters` object
  re-renders it without `Refresh()`; call `Refresh()` only to re-fetch data.

You can build it two ways:

### 1. Attribute-driven (recommended) — `TableDataCollectionFactory`

Make your row type implement `ITableDataItem` and annotate the columns:

```csharp
using Maviray.Blazor.Components.Core.Attributes;
using Maviray.Blazor.Components.Core.Models.Tables;

public class UserRow : ITableDataItem
{
    public int Id { get; init; }                 // ITableDataItem
    public string? Guid => Id.ToString();         // ITableDataItem
    public IEnumerable<MaviTableRowContextMenuItem> ContextMenu { get; init; } = [];

    [TableColumn(1)]                  public string Name { get; init; } = "";
    [Display(Name = "E-mail")]
    [TableColumn(2)]                  public string Email { get; init; } = "";
    [TableColumn(3, "dd.MM.yyyy")]    public DateTime JoinedOn { get; init; }
    [TableColumn(4, "Yes", "No")]     public bool IsActive { get; init; }
    [TableColumn(5, HorizontalPosition.Right)] public decimal Balance { get; init; }
}
```

```csharp
Func<Task<TableDataCollection>> fetch = () =>
    Task.FromResult(TableDataCollectionFactory.FromDataSource(_users));
```

`FromDataSource<T>(IEnumerable<T>)` reflects over `[TableColumn]` properties,
orders columns by `Sequence`, derives titles from `[Display]` (else the property
name), infers each column's `TableColumnDataType`, and formats values
(bool → positive/negative labels, DateTime → `DatTimeFormat`, enums →
`[Display]` name). `ContextMenu` on each item becomes that row's actions. It
keeps working in trimmed Blazor WebAssembly builds: the row type's public
properties are preserved for the reflection, as long as you call it with the
concrete row type.

**`[TableColumn]` attribute** ctors (all take a `sequence` first):

| Option | Type | Default | Purpose |
|--------|------|---------|---------|
| `Sequence` | `int` | — | Column order (required). |
| `IsNavigational` | `bool` | `false` | Marks a link/navigation column. |
| `DatTimeFormat` | `string` | `"dd.MM.yyyy"` | Date/time format. |
| `HorizontalTextAlignment` | `HorizontalPosition` | `Center` | Cell alignment. |
| `boolPositive` / `boolNegative` | `string` | `""` | Text for `true`/`false` bool cells. |

`TableDataCollectionFactory.ExportToCsv(collection)` returns a CSV string of the
visible columns/rows (properly escaped).

### 2. Manual

Construct `TableDataCollection { Columns = [...], Rows = [...] }` yourself, where
`MaviTableColumn` has `Key`, `Title`, `Sequence`, `Visible`, `DataType`,
`HorizontalTextAlignment`, and `MaviTableRow` has `Id`, `Guid`, `Cells`
(`MaviTableCell { ColumnKey, Value, OriginalValue, ColumnType }`), and
`ContextActions`.

**Reading a row's cells** (e.g. in `OnRowClick`): `row.GetCell("Email")` returns
that column's `MaviTableCell?` (`Value` is the display text), and
`row.GetCellValue("Email")` returns its `OriginalValue`. Column keys are the
`MaviTableColumn.Key` values; with `FromDataSource` that's the property name.

## Context-menu models

- **Toolbar** (`MaviTableContextMenuItem`): `Id`, `Title`, `Disabled?`. Returned
  from `ConstructContextMenu`; clicks raise `OnMainContextMenuClick`.
- **Per-row** (`MaviTableRowContextMenuItem`): `RowId`, `RowGuid`, `Id`, `Guid`,
  `Key`, `Title`, `Icon`, `Disabled`, plus `ElementVariant`, `ThemeColorScheme`,
  `ElementSize` (used when displayed as icons). Set on each row item's
  `ContextMenu`; clicks raise `OnRowContextMenuClick`.

## Full example

```razor
<MaviInMemoryTable @ref="_table"
                   FetchData="LoadAsync"
                   ConstructContextMenu="BuildToolbarAsync"
                   Parameters="_params"
                   OnRowClick="OnRowClick"
                   OnRowContextMenuClick="OnRowAction"
                   OnMainContextMenuClick="OnToolbarAction">
    <NoDataContent><div class="p-6 text-center">Nothing here yet.</div></NoDataContent>
</MaviInMemoryTable>

@code {
    private MaviInMemoryTable _table = default!;
    private readonly TableParameters _params = new()
    {
        EnableFilters = true,
        Striped = true,
        TableRowContextMenuDisplayStyle = TableRowContextMenuDisplayStyle.Icons,
    };

    Task<TableDataCollection> LoadAsync()
        => Task.FromResult(TableDataCollectionFactory.FromDataSource(_users));

    Task<IEnumerable<MaviTableContextMenuItem>> BuildToolbarAsync()
        => Task.FromResult<IEnumerable<MaviTableContextMenuItem>>(
            [ new() { Id = "export", Title = "Export CSV" } ]);

    void OnRowClick(TableClickData e) { var row = e.Row; var col = e.Column; /* … */ }
    void OnRowAction(MaviTableRowContextMenuItem a) { /* a.Key / a.RowId … */ }
    void OnToolbarAction(MaviTableContextMenuItem a) { /* a.Id == "export" … */ }
}
```

Swap `MaviInMemoryTable` for `MaviCssGridTable` with no other changes to switch
rendering strategy.

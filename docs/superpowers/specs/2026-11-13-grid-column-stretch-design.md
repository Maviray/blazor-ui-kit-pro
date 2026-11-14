# Design: Full-width grids, stretching columns, and the next-page arrow

Date: 2026-11-13 (reviewed 2026-11-14)
Status: Reviewed — ready to implement
Target release: the next release after 0.3.7

## Principle

As with access policies, the library supplies the generic mechanism and the
consuming app decides. MaviGrid decides how columns share the width. Each app
decides which columns stretch, through the grid model or per grid instance.

## Problem

Both problems were found in a consuming app's grid on 0.3.7. The grid has two
columns, Code and Name, in a full-width card.

1. **The grid uses only part of the available width.**
   - Cause: `MaviGridLayout` (`MaviGridLayout.cs`) builds
     `grid-template-columns: repeat({columnCount}, minmax(min-content, 200px))`.
     Every column, including the trailing actions (context-menu) column, is capped
     at 200px.
   - The container (`MaviGrid.razor:14`) is `inline-block max-w-full`, so on its
     own it shrinks to that width. In a stretching parent the border fills the
     parent, but the columns still end after about N × 200px.
   - Result: a wide card with the columns pushed to the left and most of the row
     empty. No attribute or parameter changes this.
2. **The next-page arrow points left.**
   - Cause: 0.3.7 renders the next button as `lni lni-chevron-left lni-flip-x`.
   - `lni-flip-x` is defined in none of these: the free LineIcons 5.0 CDN
     stylesheet, the LineIcons Pro 5.1 stylesheets, `maviray.grid.min.css`, or
     `maviray.material.min.css`. The chevron is therefore never mirrored, and the
     next button shows the same icon as the previous button. The fix in `2ec6429`
     assumed a flip utility that doesn't exist.

## Goals

1. By default the grid fills the width of its container.
2. An app can mark one or more columns as stretching. Stretching columns share the
   free width, optionally in proportion to a weight.
3. **Default when no visible column is marked:** data columns keep today's sizing,
   and the trailing actions column takes the free width. A grid with no stretch
   configuration then fills its container, with the actions menu at the far right.
4. **Per-grid override:** two pages that use the same grid model can stretch
   different columns.
5. The next-page arrow points right with LineIcons Pro.
6. Sorting, filtering, states, selection, menus and persisted prerender pages
   behave as today. Any change to them is additive.

## Non-goals

- Fixed, minimum or maximum widths per column. Non-stretching columns keep
  `minmax(min-content, 200px)`. A later `[GridColumnWidth]` could add this.
- Columns the user can resize.
- Moving or changing the actions column.
- Making the pager icons configurable.

## Design

### 1. Marking stretching columns

```csharp
namespace Maviray.Blazor.Components.Pro.Core.Attributes.Grid;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GridStretchAttribute(int weight = 1) : Attribute
{
    /// <summary>The column's share of the free width relative to other stretching columns. At least 1.</summary>
    public int Weight { get; } = weight;
}
```

- **Where it applies:**
  - on a `[GridDataProperty]` property;
  - on a `[GridNestedProperty]` member, whose column is keyed `Nav.Member`;
  - on a `[GridNavProperty]` property, where it applies to each of that
    navigation's nested columns. A nested member's own `[GridStretch]` overrides it.
- **Where it's ignored:** on a property that isn't a column.
- **Invalid weight:** a weight below 1 is a configuration error. `GridModelMetadata`
  throws `InvalidOperationException` while it discovers the model's columns, the
  first time any page or column list is built for that model. The message names
  the model type, the property the attribute is on and the weight. The metadata's
  `Lazy` caches the exception, so every later fetch for that model fails the same
  way, and a grid shows its fetch-error state.
- **Carried on the column:** `MaviGridColumn` gains `int StretchWeight { get; init; }`
  (0 means it doesn't stretch). `GridModelMetadata` sets it on the column descriptor
  next to `Sequence`, `CellBehavior` and `GraphicDisplay`. It is copied everywhere
  those are copied:
  - `GridColumnDescriptor.ToColumn` (`GridModelMetadata.cs`), which builds every
    column and the denied page's columns;
  - `MaviGridColumn.WithIndex` (`MaviGridColumn.cs`), which re-indexes the columns
    left when an access policy hides some. Without it, a policy-hidden column set
    would silently lose every stretch mark.
- It is part of the persisted prerender page (additive).

### 2. Per-grid override

- `MaviGridParameters.StretchColumns`: `IReadOnlyDictionary<string, int>?`, mapping
  column key to weight. The default is `null`.
- `null` means the model's `[GridStretch]` marks apply.
- When it is set, it replaces the attribute marks for that grid instance:
  - a key in the dictionary stretches with its weight; any other column doesn't;
  - an empty dictionary means no data column stretches, so the default rule
    applies and the actions column stretches;
  - keys are looked up with the dictionary's own comparer;
  - unknown keys are ignored, like sort and filter keys that match no column.
- **When it applies:** like `Condensed` and `Striped`. The grid compares the
  dictionary by reference whenever its parameters are set, and rebuilds the layout
  when it changed. To change it, assign a new dictionary. A dictionary changed in
  place is not detected.
- **Invalid weight:** a weight below 1 throws `ArgumentOutOfRangeException`
  (parameter `StretchColumns`, naming the key and the weight) when the grid's
  parameters are set, not while it renders.

### 3. The layout rule (`MaviGridLayout`)

Replace `repeat(...)` with an explicit list with one track per rendered column, in
render order: the visible data columns, the select column when shown, then the
actions column.

| Track | Template |
|---|---|
| Visible data column with weight `w` > 0 | `minmax(min-content, {w}fr)` |
| Visible data column with weight 0 | `minmax(min-content, 200px)` (unchanged) |
| Select column | `minmax(min-content, 200px)` (unchanged) |
| Actions column, when no visible data column stretches | `minmax(min-content, 1fr)` |
| Actions column, when one or more visible data columns stretch | `min-content` |

- The tracks are joined with single spaces:
  `grid-template-columns: minmax(min-content, 200px) minmax(min-content, 1fr) min-content`.
- **"Visible"** means after both kinds of hidden column are removed: columns the
  user hides (the column dialog or `SetColumnVisible`) and columns an access policy
  hides (`WithHiddenColumns`, which leaves them out of the page). If every marked
  column is hidden, the actions column stretches again.
- **The header, filter, data and footer rows** already share the one CSS grid, so
  they line up with no further change. The full-row states (no data, denied) use
  `col-span-full` and keep it.
- **Narrow containers:** when the columns' minimum widths don't fit, the container
  scrolls horizontally, as today (`overflow:auto`).

### 4. Filling the container

- `MaviGridParameters.FillWidth`: `bool`, default `true`.
  - `true`: the container is `block w-full` instead of `inline-block max-w-full`.
  - `false`: today's shrink-to-fit container. In a shrink-to-fit container,
    `fr` tracks size to their content, so stretching has no visible effect.
- It is a parameter, not a consumer `Class`, because `block` and `inline-block`
  set on one element resolve by stylesheet order, not class order. A class
  override would be unreliable.

### 5. The next-page arrow

- The next button uses `Icon="lni lni-chevron-right"`; the previous button keeps
  `lni lni-chevron-left`.
- `lni-chevron-right` is in LineIcons Pro 5.1 but not in the free LineIcons 5.0 CDN
  stylesheet, which defines only `lni-chevron-right-circle`. With the free
  stylesheet the next button shows no glyph; its tooltip and `aria` label still
  work. The grid's pager therefore expects LineIcons Pro.
- The sample app loads the free CDN stylesheet, so its next button is blank until
  it loads a Pro stylesheet.
- Replace the comment at `MaviGrid.razor:190`, which still describes the mirrored
  left chevron.
- Docs (README, `mavi-grid` skill, sample host-setup notes) say the pager's
  next-page icon needs LineIcons Pro.

## Compatibility and versioning

Visible changes for an app that configures nothing:

- **The grid fills its container's width.** The actions column takes the free
  space, so the header menu button sits at the right edge.
- **The next-page arrow points right** with LineIcons Pro; with free LineIcons 5.0
  the next button has no glyph.

Additive API:

- `GridStretchAttribute`, `MaviGridColumn.StretchWeight`,
  `MaviGridParameters.StretchColumns` and `MaviGridParameters.FillWidth`.
- A persisted page written by the previous version restores with
  `StretchWeight = 0` on every column.

Nothing is source-breaking.

## Tests

**`Maviray.Blazor.Components.Pro.Grid.UI.Tests`** (bUnit), asserting on the inner
grid element's `grid-template-columns` and the container's classes:

- **Defaults:**
  - no marks: data columns `minmax(min-content, 200px)`, actions column
    `minmax(min-content, 1fr)`;
  - the container has `block w-full`.
- **Attribute marks** (columns with `StretchWeight`):
  - one stretching column: that column is `1fr` and the actions column is
    `min-content`;
  - two columns weighted 1 and 2: `1fr` and `2fr`;
  - with the select column shown, the track count and order are right.
- **Override:**
  - `StretchColumns` replaces the attribute marks;
  - an empty dictionary makes the actions column stretch again;
  - unknown keys are ignored;
  - a weight below 1 throws `ArgumentOutOfRangeException`;
  - a new dictionary passed in new parameters takes effect without a fetch.
- **Hidden columns:**
  - a user-hidden stretching column contributes no track;
  - when all marked columns are hidden, the actions column stretches;
  - a column missing from the page (as an access policy leaves it out) that
    `StretchColumns` names: the actions column stretches.
- **`FillWidth = false`:** the container is `inline-block max-w-full`.
- **Next-page arrow:** the next button's icon is `lni-chevron-right`, the previous
  button's is `lni-chevron-left`, and neither uses `lni-flip-x`.
- **Persisted page:** a prerendered page with a stretching column restores in the
  interactive grid with the same tracks and no second fetch.

**`Maviray.Blazor.Components.Pro.Core.Tests`:**

- **Metadata:**
  - `[GridStretch]` sets `StretchWeight` on data properties, nested members and
    navigation properties, and unmarked columns are 0;
  - a nested member's own mark wins over the navigation's;
  - columns left after hiding some (`WithIndex`) keep their weights;
  - a weight below 1 throws `InvalidOperationException`, naming the model and the
    property.
- **JSON:** a `MaviGridColumn` with `StretchWeight` round-trips, and JSON without
  it reads as 0.

**Sample:**

- On the `/docs` page, a grid with no marks (actions column stretches), a grid with
  one stretching column, and a grid with weights.
- Check the next arrow by hand with a LineIcons Pro stylesheet.

## Docs

- **`mavi-grid` skill:**
  - add `[GridStretch]`, `StretchColumns` and `FillWidth` to `references/api.md`
    and to a recipe;
  - correct the icon note: the pager's next icon needs LineIcons Pro.
- **README:** the same.
- **Sample `/docs` reference tables** (`DocReference.cs`): the same rows.

## How an app uses it (informative)

- Mark a long text column such as a supplier `Name` with `[GridStretch]`, so long
  names show in full and the actions column stays narrow.
- Other grids decide per model. With no marks, the default (actions column
  stretches) already fills the card.

## Decisions (confirmed in review)

- **The default stretches the actions column**, not the last data column. Data
  columns keep today's 200px cap unless marked.
- **The weight is optional** and defaults to 1. A plain `[GridStretch]` covers
  "one or more columns stretch equally".
- **A per-grid override exists** (`StretchColumns`), so one model can serve both a
  compact and a full page.
- **`FillWidth` defaults to `true`**, a visible change for existing apps. Today's
  look is one parameter away.
- **The next arrow is `lni-chevron-right`**, so the pager expects LineIcons Pro. A
  transform-mirrored left chevron, which would work with the free set too, was
  considered and not chosen.

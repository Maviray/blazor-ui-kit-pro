# Grid Column Stretch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** MaviGrid fills its container by default, lets apps mark stretching columns (by attribute or per grid), and shows a right-pointing next-page arrow.

**Architecture:** Core carries a stretch weight on each `MaviGridColumn`, read from `[GridStretch]` by `GridModelMetadata`. The grid UI turns the visible columns' weights, or a per-grid `StretchColumns` override, into an explicit `grid-template-columns` track list in `MaviGridLayout`. A `FillWidth` parameter picks the container's display classes.

**Tech Stack:** .NET 10, Blazor, Tailwind 4 (grid CSS built from the Razor/C# sources), xUnit v3, bUnit 2.15.

**Spec:** `docs/superpowers/specs/2026-11-13-grid-column-stretch-design.md`

## Global Constraints

- This is a public repo. No file, commit or PR names the private consuming app: refer to it as "a consuming app". Before each commit, check the staged diff for its name.
- Track strings, exactly: weighted `minmax(min-content, {w}fr)`, fixed `minmax(min-content, 200px)`, select `minmax(min-content, 200px)`, actions `minmax(min-content, 1fr)` when no visible data column stretches, otherwise `min-content`. Joined by single spaces after `grid-template-columns: `.
- Container classes, exactly: `block w-full` when `FillWidth` (default `true`), else `inline-block max-w-full`.
- Next-page icon stays `lni lni-chevron-right` (already in the working tree); previous stays `lni lni-chevron-left`.
- Additive API only: `GridStretchAttribute`, `MaviGridColumn.StretchWeight`, `MaviGridParameters.StretchColumns`, `MaviGridParameters.FillWidth`. Nothing source-breaking.
- Run tests with `dotnet test --project <csproj>` (Microsoft.Testing.Platform; the positional form fails). Baseline: Grid.UI.Tests 72 passed, Core.Tests 188 passed.
- Doc comments in the repo's voice: short, plain sentences, `<see cref>` where existing comments use them.

## Review Focus

1. **Access-denied page:** the denied page's columns come from `DeniedColumns` (`ToColumn`), so its header tracks must match the allowed page's. Test in Task 1.
2. **`StretchColumns` with a case-insensitive comparer:** keys are looked up with the dictionary's own comparer, so `new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["name"] = 1 }` stretches `Name`. Test in Task 2.
3. **Paging keeps the tracks:** columns are reused across fetches and the layout isn't rebuilt, so the next page must keep the stretched tracks. Test in Task 2.
4. **Row actions as icons with a stretching column:** the actions column shrinks to `min-content`, so several icon buttons must stay on one line. Hand check in the sample in Task 4.
5. **Free LineIcons:** the next button has no glyph but must keep its tooltip (`GoToNextPageText`). Test in Task 3.

---

### Task 1: Stretch weights on columns (Core)

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Core/Attributes/Grid/GridStretchAttribute.cs`
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Models/Grid/MaviGridColumn.cs` (property, `WithIndex`)
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Services/GridModelMetadata.cs` (`Discover`, `Describe`, `GridColumnDescriptor`, `ToColumn`)
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GridColumnStretchTests.cs`

**Interfaces:**
- Produces: `GridStretchAttribute(int weight = 1)` with `int Weight { get; }` (namespace `Maviray.Blazor.Components.Pro.Core.Attributes.Grid`, sealed, `AttributeUsage(Property, AllowMultiple = false)`, code exactly as in spec §1).
- Produces: `MaviGridColumn.StretchWeight` — `public int StretchWeight { get; init; }`, 0 = doesn't stretch. Doc comment: the column's share of the free width relative to other stretching columns; 0 for a column that keeps its width.
- Produces: `GridColumnDescriptor.StretchWeight` — `public required int StretchWeight { get; init; }`.

- [ ] **Step 1: Write the failing tests**

Test models as private nested classes in the test class, over the existing `TestOrder` fixture (`Id`, `Name`, `Secret`, `Vendor` of `TestVendor` with `Code`, `IsPreferred`):

- `StretchVendorModel`: `[GridNestedProperty("Code")] [GridStretch(3)] string? Code`, then `[GridNestedProperty("Preferred")] bool IsPreferred`.
- `StretchModel : IGridCollectionModel<TestOrder, int>`: `[GridDataProperty(1)] int Id`; `[GridDataProperty(2)] [GridStretch] string? Name`; `[GridNavProperty(3)] [GridStretch(2)] StretchVendorModel? Vendor`; `[GridStretch(5)] string? Secret` (not a column); `Key => Id`; empty `ConstructFrom`; `GetContextActions() => []`.
- `ZeroWeightModel : IGridCollectionModel<TestOrder, int>`: `[GridDataProperty(1)] int Id`; `[GridDataProperty(2)] [GridStretch(0)] string? Name`.

Helper: `static Dictionary<string, int> Weights(IEnumerable<MaviGridColumn> columns) => columns.ToDictionary(c => c.Key, c => c.StretchWeight);`

```csharp
[Fact]
public void Weights_ComeFromGridStretch_OnDataNestedAndNavigationProperties()
{
    var columns = GridPageBuilder<TestOrder, StretchModel, int>.Columns();

    // Vendor.Code's own mark (3) wins over the navigation's (2); Vendor.IsPreferred takes the navigation's.
    Assert.Equal(new Dictionary<string, int> { ["Id"] = 0, ["Name"] = 1, ["Vendor.Code"] = 3, ["Vendor.IsPreferred"] = 2 }, Weights(columns));
}

[Fact]
public void ColumnsLeftAfterHiding_KeepTheirWeights()
{
    var columns = GridPageBuilder<TestOrder, StretchModel, int>.Columns(hiddenColumns: ["Id"]);

    Assert.Equal(new Dictionary<string, int> { ["Name"] = 1, ["Vendor.Code"] = 3, ["Vendor.IsPreferred"] = 2 }, Weights(columns));
    Assert.Equal([0, 1, 2], columns.Select(c => c.Index));
}

[Fact]
public void DeniedPageColumns_KeepTheirWeights()
{
    var page = GridPageBuilder<TestOrder, StretchModel, int>.Denied();

    Assert.Equal(1, page.Columns.Single(c => c.Key == "Name").StretchWeight);
    Assert.Equal(3, page.Columns.Single(c => c.Key == "Vendor.Code").StretchWeight);
}

[Fact]
public void WeightBelowOne_Throws_NamingTheModelAndTheProperty()
{
    var error = Assert.Throws<InvalidOperationException>(() => GridPageBuilder<TestOrder, ZeroWeightModel, int>.Columns());

    Assert.Contains(nameof(ZeroWeightModel), error.Message);
    Assert.Contains("Name", error.Message);
    Assert.Contains("0", error.Message);
}

[Fact]
public void StretchWeight_RoundTripsThroughJson_AndIsZeroWhenAbsent()
{
    var column = new MaviGridColumn { Key = "Name", Title = "Name", StretchWeight = 2 };

    Assert.Equal(2, JsonSerializer.Deserialize<MaviGridColumn>(JsonSerializer.Serialize(column))!.StretchWeight);
    Assert.Equal(0, JsonSerializer.Deserialize<MaviGridColumn>("""{"Key":"Name","Title":"Name"}""")!.StretchWeight);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --project src/Maviray.Blazor.Components.Pro.Core.Tests/Maviray.Blazor.Components.Pro.Core.Tests.csproj`
Expected: build fails, with `GridStretch` and `StretchWeight` not found.

- [ ] **Step 3: Implement**

- Add `GridStretchAttribute` (spec §1), `MaviGridColumn.StretchWeight`, and copy it in `MaviGridColumn.WithIndex`.
- `GridColumnDescriptor`: add the required `StretchWeight` and copy it in `ToColumn`.
- `Describe(...)`: take an `int stretchWeight` parameter and set it on the descriptor.
- `Discover()`: a data property's weight is its own `[GridStretch]`, or 0. A nested member's weight is its own `[GridStretch]`, else the navigation property's, else 0.
- Validate each `[GridStretch]` where it is read (data property, navigation property, nested member). If `Weight < 1`, throw `InvalidOperationException` with a message naming `typeof(TModel).Name`, the property (`Name`, `Vendor` or `Vendor.Code`) and the weight. For example: `"StretchModel.Name: [GridStretch] weight must be at least 1, not 0."`

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test --project src/Maviray.Blazor.Components.Pro.Core.Tests/Maviray.Blazor.Components.Pro.Core.Tests.csproj`
Expected: 193 passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core src/Maviray.Blazor.Components.Pro.Core.Tests
git commit -m "feat(grid): stretch weights on grid columns from [GridStretch]"
```

---

### Task 2: Stretching tracks, per-grid override and full-width container (Grid UI)

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGridParameters.cs`
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGridLayout.cs` (the constructor's `GridStyle`)
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor.cs` (`_appliedSettings`, `ApplySettings`)
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor` (container classes at line 14; `data-grid-body` on the inner grid at line 65)
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.UI.Tests/Fixtures/GridPages.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Grid.UI.Tests/MaviGridColumnStretchTests.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Grid.UI.Tests/MaviGridPrerenderStateTests.cs` (one test)

**Interfaces:**
- Consumes: `MaviGridColumn.StretchWeight` (Task 1).
- Produces: `MaviGridParameters.StretchColumns` (`IReadOnlyDictionary<string, int>?`, default `null`) and `MaviGridParameters.FillWidth` (`bool`, default `true`), with doc comments that follow spec §2 and §4, including "assign a new dictionary to change it".
- Produces: test fixtures `GridPages.Stretched(params (string Key, int Weight)[] weights) → IReadOnlyList<MaviGridColumn>` (copies of `GridPages.Columns` with those weights; every other field copied) and `GridPages.PageWith(IReadOnlyList<MaviGridColumn> columns, MaviGridQuery query, int totalRows, params int[] keys) → MaviGridCollection<int>` (like `PageOf`, with one cell per column `"{column.Key} {key}"` in `Index` order, and `columns` passed through by reference).
- Produces: markup `data-grid-body` on the inner `.grid` element.

- [ ] **Step 1: Write the failing tests**

In `MaviGridColumnStretchTests : MaviGridTestContext`:

```csharp
private const string Fixed = "minmax(min-content, 200px)";
private const string Fill = "minmax(min-content, 1fr)";

private static string Tracks(IRenderedComponent<MaviGrid<int>> grid) =>
    grid.Find("[data-grid-body]").GetAttribute("style")!.Replace("grid-template-columns: ", "");

private static MaviGridParameters With(IReadOnlyDictionary<string, int>? stretch = null, bool select = false, bool fill = true) =>
    new() { FilterDebounceMilliseconds = 0, StretchColumns = stretch, DisplaySelectColumn = select, FillWidth = fill };
```

The fetch for weighted columns is `new RecordingFetch((q, _) => GridPages.PageWith(columns, q, 1, 1))`, where `columns` is captured once per test.

| Test | Setup | Assertion |
|---|---|---|
| `NoMarks_DataColumnsKeepTheirWidth_AndTheActionsColumnFills` | `GridPages.Columns` | `Tracks == $"{Fixed} {Fixed} {Fixed} {Fill}"` |
| `Container_FillsItsParent_ByDefault` | default | `[data-grid]` classes contain `block`, `w-full`; not `inline-block`, `max-w-full` |
| `FillWidthOff_ShrinksTheContainerToItsColumns` | `With(fill: false)` | classes contain `inline-block`, `max-w-full`; not `block`, `w-full` |
| `OneStretchingColumn_TakesTheFreeWidth_AndTheActionsColumnShrinks` | `Stretched(("Name", 1))` | `$"{Fixed} minmax(min-content, 1fr) {Fixed} min-content"` |
| `Weights_SplitTheFreeWidth` | `Stretched(("Id", 1), ("Name", 2))` | `$"minmax(min-content, 1fr) minmax(min-content, 2fr) {Fixed} min-content"` |
| `SelectColumn_HasItsTrack_BeforeTheActionsColumn` | `Stretched(("Name", 1))`, `With(select: true)` | `$"{Fixed} minmax(min-content, 1fr) {Fixed} {Fixed} min-content"` |
| `StretchColumns_ReplacesTheColumnMarks` | `Stretched(("Name", 1))`, `With(new Dictionary<string, int> { ["Note"] = 3 })` | `$"{Fixed} {Fixed} minmax(min-content, 3fr) min-content"` |
| `EmptyStretchColumns_LetsTheActionsColumnFill` | `Stretched(("Name", 1))`, `With(new Dictionary<string, int>())` | `$"{Fixed} {Fixed} {Fixed} {Fill}"` |
| `StretchColumns_IgnoresUnknownKeys` | `GridPages.Columns`, `{ ["Missing"] = 2, ["Name"] = 1 }` | `$"{Fixed} minmax(min-content, 1fr) {Fixed} min-content"` |
| `StretchColumns_UsesTheDictionarysComparer` | `GridPages.Columns`, `new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["name"] = 1 }` | Name is `1fr` (same tracks as the row above) |
| `StretchWeightBelowOne_Throws` | `{ ["Name"] = 0 }` | `Assert.Throws<ArgumentOutOfRangeException>(() => RenderGrid(...))`, `ParamName == "StretchColumns"` |
| `NewStretchColumns_ApplyWithoutAFetch` | render with `With()`, then `grid.Render(b => b.Add(x => x.Parameters, With(new Dictionary<string, int> { ["Name"] = 1 })))` | tracks have Name `1fr` and `min-content`; `fetch.Queries` count is still 1 |
| `UserHiddenStretchingColumn_HasNoTrack` | `Stretched(("Id", 1), ("Name", 2))`, `grid.InvokeAsync(() => grid.Instance.SetColumnVisible("Id", false))` | `$"minmax(min-content, 2fr) {Fixed} min-content"` |
| `AllMarkedColumnsHidden_LetTheActionsColumnFill` | `Stretched(("Name", 1))`, hide `Name` | `$"{Fixed} {Fixed} {Fill}"` |
| `StretchColumnLeftOutOfThePage_LetsTheActionsColumnFill` | page columns `[Id, Note]` only (as a policy leaves `Name` out; `Index` 0 and 1), `{ ["Name"] = 1 }` | `$"{Fixed} {Fixed} {Fill}"` |
| `NextPage_KeepsTheStretchedTracks` | `Stretched(("Name", 1))`, 30 rows via `PageWith(columns, q, 30, …)`, click `[data-page-next] button` | tracks unchanged after the second fetch |

In `MaviGridPrerenderStateTests`:

```csharp
[Fact]
public void RestoredPage_KeepsItsStretchingColumns()
{
    var columns = GridPages.Stretched(("Name", 1));
    var fetch = new RecordingFetch((q, _) => GridPages.PageWith(columns, q, 1, 1));
    Prerender(fetch);

    var grid = StartInteractive(fetch);

    Assert.Single(fetch.Queries);
    Assert.Contains("minmax(min-content, 1fr) minmax(min-content, 200px) min-content", grid.Find("[data-grid-body]").GetAttribute("style"));
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test --project src/Maviray.Blazor.Components.Pro.Grid.UI.Tests/Maviray.Blazor.Components.Pro.Grid.UI.Tests.csproj`
Expected: build fails, with `StretchColumns`, `FillWidth` and `StretchWeight` not found.

- [ ] **Step 3: Implement**

- `MaviGridParameters`: the two properties.
- `MaviGridLayout` constructor: replace the `repeat(...)` line. The weight of a visible column is `parameters.StretchColumns is { } overrides ? (overrides.TryGetValue(column.Key, out var w) ? w : 0) : column.StretchWeight`. Tracks follow the Global Constraints, in the order data columns, then select (when `ShowSelect`), then actions. Format weights with `CultureInfo.InvariantCulture`. `MaviGridLayout.Empty` (no columns) yields `grid-template-columns: minmax(min-content, 1fr)`.
- `MaviGrid.razor.cs`: add `Parameters.StretchColumns` to the `_appliedSettings` tuple, which already marks the layout dirty on change. When the tuple changed, check every weight first, before recording the settings. On a weight below 1, throw `new ArgumentOutOfRangeException(nameof(MaviGridParameters.StretchColumns), weight, $"Column '{key}' has stretch weight {weight}; a weight must be at least 1.")`.
- `MaviGrid.razor`: the container's `inline-block max-w-full` becomes `@(Parameters.FillWidth ? "block w-full" : "inline-block max-w-full")`. Add `data-grid-body` to `<div class="grid divide-y divide-zinc-200" ...>`.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test --project src/Maviray.Blazor.Components.Pro.Grid.UI.Tests/Maviray.Blazor.Components.Pro.Grid.UI.Tests.csproj`
Expected: 89 passed (72 + 17), 0 failed.

- [ ] **Step 5: Check that the grid stylesheet has the classes**

The UI project's build runs `npm run buildcss`. Run: `dotnet build src/Maviray.Blazor.Components.Pro.Grid.UI` and then `grep -o '\.block{[^}]*}\|\.w-full{[^}]*}\|\.inline-block{[^}]*}\|\.max-w-full{[^}]*}' src/Maviray.Blazor.Components.Pro.Grid.UI/wwwroot/css/maviray.grid.min.css`
Expected: all four rules print.

- [ ] **Step 6: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Grid.UI src/Maviray.Blazor.Components.Pro.Grid.UI.Tests
git commit -m "feat(grid-ui): full-width grids with stretching columns"
```

---

### Task 3: Pin the next-page arrow (Grid UI)

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor:190` (comment only; the icon change is already in the working tree)
- Test: `src/Maviray.Blazor.Components.Pro.Grid.UI.Tests/MaviGridRenderingTests.cs`

**Interfaces:** none.

- [ ] **Step 1: Write the test**

```csharp
[Fact]
public void PagerArrows_AreTheLeftAndRightChevrons()
{
    var grid = RenderGrid(new RecordingFetch(1).Invoke);

    Assert.NotNull(grid.Find("[data-page-prev] .lni-chevron-left"));
    Assert.NotNull(grid.Find("[data-page-next] .lni-chevron-right"));
    Assert.Empty(grid.FindAll("[data-pager] .lni-flip-x"));
    // Free LineIcons 5.0 has no lni-chevron-right glyph: the button must still say what it does.
    Assert.Equal("Go to next page", grid.Find("[data-page-next] [title]").GetAttribute("title"));
}
```

If `MaviButtonIcon` puts the title on a different element, adjust only the `[title]` selector to the element that carries it, as the rendered markup shows.

- [ ] **Step 2: Run it**

Run: `dotnet test --project src/Maviray.Blazor.Components.Pro.Grid.UI.Tests/Maviray.Blazor.Components.Pro.Grid.UI.Tests.csproj`
Expected: PASS at once, because the icon change is already in. To see it fail, revert the icon locally to `lni lni-chevron-left lni-flip-x`, run the test, then restore the icon.

- [ ] **Step 3: Replace the stale comment**

`@* LineIcons 5 has no lni-chevron-right: … mirrored … *@` becomes `@* lni-chevron-right is in LineIcons Pro; the free LineIcons 5.0 set has no such glyph. *@`

- [ ] **Step 4: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor src/Maviray.Blazor.Components.Pro.Grid.UI.Tests/MaviGridRenderingTests.cs
git commit -m "fix(grid-ui): next-page arrow uses lni-chevron-right"
```

---

### Task 4: Sample demos and docs

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.Sample/Components/Docs/AppearanceSection.razor`
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.Sample/Components/Docs/DocSnippets.cs` (new `ColumnWidths` snippet)
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.Sample/Components/Docs/DocReference.cs` (`GridParameters`, `Attributes`)
- Modify: `README.md` (Columns table, `MaviGridParameters` table, Host setup)
- Modify: `.claude/skills/mavi-grid/references/api.md` (`MaviGridParameters` table, attribute table)
- Modify: `.claude/skills/mavi-grid/references/recipes.md` (icon note at lines 76-77, a column-width recipe)
- Modify: `.claude/skills/mavi-grid/SKILL.md` (LineIcons line near 63)

**Interfaces:**
- Consumes: `[GridStretch(weight)]`, `StretchColumns`, `FillWidth` (Tasks 1-2).

- [ ] **Step 1: Sample demos**

Add a "Column widths" part to `AppearanceSection`, after the existing demo and snippet. It uses the same `DocItemModel` and factory, with two more grids that differ only in `MaviGridParameters`:
- the existing demo is the no-marks case (the actions column fills); say so in one sentence;
- `StretchColumns = new Dictionary<string, int> { ["Name"] = 1 }`;
- `StretchColumns = new Dictionary<string, int> { ["Name"] = 2, ["Date"] = 1 }`.

`DocSnippets.ColumnWidths` shows `[GridStretch]` on a model property, `[GridStretch(2)]`, and the `StretchColumns` and `FillWidth = false` parameters. Don't change `DocItemModel`: a mark there would change every other section's demo.

- [ ] **Step 2: Reference rows**

- `DocReference.GridParameters`: `StretchColumns`, `IReadOnlyDictionary<string, int>?`, `null`, "Column key to weight: these columns share the free width and replace the model's [GridStretch] marks. Empty: none stretch. Assign a new dictionary to change it." `FillWidth`, `bool`, `true`, "The grid fills its container's width. false: it shrinks to its columns."
- `DocReference.Attributes`: `[GridStretch(weight = 1)]`, "column property, navigation property", "", "The column takes a share of the free width, in proportion to its weight. On a navigation property, it applies to its nested columns."

- [ ] **Step 3: README and skill**

- README Columns table: a `[GridStretch(weight = 1)]` row. `MaviGridParameters` table: `StretchColumns` and `FillWidth` rows. Below the table, one sentence on the default: with no stretching column, the actions column takes the free width.
- README Host setup and `SKILL.md`: after the LineIcons `<link>`, a note that the pager's next-page icon (`lni-chevron-right`) is in LineIcons Pro, not in the free 5.0 CDN stylesheet.
- `api.md`: the same rows in its `MaviGridParameters` and attribute tables.
- `recipes.md` lines 76-77: replace "there is no `lni-chevron-right` in LineIcons 5" with "the free LineIcons 5.0 set has no `lni-chevron-right` (Pro has it)". Add a short "Column widths" recipe with the same three variants as the sample.

- [ ] **Step 4: Build and check by hand**

Run: `dotnet build Maviray.Blazor.Components.Pro.slnx`
Expected: 0 errors, no new warnings.

Run the sample (`dotnet run --project src/Maviray.Blazor.Components.Pro.Grid.Sample`) and open `/docs#appearance`. Expected:
- each grid fills the demo box;
- the no-marks grid has its ⋯ at the right edge;
- `Name` takes the free width in the second grid, and `Name` takes twice `Created`'s extra width in the third;
- on a row-actions-as-icons grid (the row actions section), the icons stay on one line;
- the next-page button is blank with the free CDN stylesheet and shows a right chevron with a LineIcons Pro stylesheet.

- [ ] **Step 5: Full test run and commit**

Run both test projects (commands as in Tasks 1 and 2). Expected: Core 193 passed, UI 90 passed.

```bash
git add README.md .claude/skills/mavi-grid src/Maviray.Blazor.Components.Pro.Grid.Sample docs/superpowers
git commit -m "docs(grid): column widths, StretchColumns and FillWidth"
```

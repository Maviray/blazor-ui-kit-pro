# Grid Sort/Filter Property Mapping Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let `GenericGridDataFactory<TDataItem, TModel, TKey>` sort/filter `TModel` properties that are built with custom logic in `ConstructFrom` (not a direct 1:1 property match with `TDataItem`), by resolving them through an optional explicit path map, falling back to today's identical-name behavior, and degrading to "not sortable/not filterable" instead of throwing when neither resolves.

**Architecture:** A new `GridPropertyPathMap<TDataItem, TModel>` lets a consumer register `TModel` property → `TDataItem` dot-path mappings (with an optional null-coalescing default). `GenericGridDataFactory` resolves every sort/filter request and every grid column through this map first, then an identical-name check, and marks columns `Sortable`/`Filterable` accordingly. A shared `GridPropertyExpressionBuilder` teaches both `QueryableExtensions.OrderByDynamic` and `QueryBuilder.BuildFilterExpression` to wrap a resolved property access in `Expression.Coalesce` when a null default is supplied, so a nullable navigation property sorts/filters at the database exactly the way `ConstructFrom`'s `?? default` coalesces it in memory.

**Tech Stack:** .NET 10 / C#, EF Core (consumer-facing only, not exercised by these tests), xUnit (new test project — none exists in this repo today), LINQ Expression trees.

**Design doc:** `docs/2026-07-01-grid-sort-filter-property-mapping-design.md`

## Global Constraints

- Target framework for all new/modified projects: `net10.0` (matches every existing `.csproj` in this solution).
- `Nullable` and `ImplicitUsings` stay `enable` (matches existing project conventions).
- No breaking change to `IGridCollectionModel<TDataItem, TKey>` — `ConstructFrom` stays exactly as it is today.
- Every new parameter added to an existing public method/interface must be optional with a `null` default, so every existing call site in this repo (and any external consumer) keeps compiling unchanged.
- Scope is bounded to top-level `[GridDataProperty]` columns. `[GridNavProperty]` nested columns are unaffected by this plan — their `Sortable`/`Filterable` stay at the `MaviGridColumn` default (`true`), matching today's unchecked behavior exactly.
- A sort or filter request that cannot be resolved to a `TDataItem` path must never throw. Sort falls back to the existing default order column (`QueryConstants.ID`, i.e. `"Id"`); an unresolvable filter term is dropped.
- Tests use `List<T>.AsQueryable()` fakes, not a real database — LINQ-to-Objects fully executes `Expression.Coalesce` and dotted `Expression.PropertyOrField` chains, so no EF Core test dependency is needed anywhere in this plan.

---

### Task 1: Test project scaffolding

No test project exists in this solution yet. This task creates one, wires it into the `.slnx`, and proves the wiring with one real (non-trivial) assertion against the Core assembly before any feature code is added.

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Core.Tests/Maviray.Blazor.Components.Pro.Core.Tests.csproj`
- Modify: `Maviray.Blazor.Components.Pro.slnx`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/SmokeTests.cs`

**Interfaces:**
- Produces: an xUnit test project named `Maviray.Blazor.Components.Pro.Core.Tests`, referencing `Maviray.Blazor.Components.Pro.Core.csproj`, runnable via `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests`. Later tasks add fixtures/tests under this project.

- [ ] **Step 1: Create the test project file**

`src/Maviray.Blazor.Components.Pro.Core.Tests/Maviray.Blazor.Components.Pro.Core.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.7.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Maviray.Blazor.Components.Pro.Core\Maviray.Blazor.Components.Pro.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Register the project in the solution**

Modify `Maviray.Blazor.Components.Pro.slnx` — add a new `<Project>` line (any valid GUID works; use `e32fbe0b-8273-4d5c-bee9-f2762deecede` below):

```xml
<Solution>
  <Project Path="src/Maviray.Blazor.Components.Pro.Core/Maviray.Blazor.Components.Pro.Core.csproj" Id="3624ea4f-890c-4d15-8cd7-fa74c6185729" />
  <Project Path="src/Maviray.Blazor.Components.Pro.Core.Tests/Maviray.Blazor.Components.Pro.Core.Tests.csproj" Id="e32fbe0b-8273-4d5c-bee9-f2762deecede" />
  <Project Path="src/Maviray.Blazor.Components.Pro.Grid.EntityFramework/Maviray.Blazor.Components.Pro.Grid.EntityFramework.csproj" Id="e56468de-97e3-4178-9d6e-6f54bb0c2c35" />
  <Project Path="src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Maviray.Blazor.Components.Pro.Grid.Sample.Database.csproj" Id="a4766501-5d36-429c-ab45-7353fce2a95f" />
  <Project Path="src/Maviray.Blazor.Components.Pro.Grid.Sample/Maviray.Blazor.Components.Pro.Grid.Sample.csproj" Id="0b90d85c-f1b4-4414-a792-303bb4e01d15" />
  <Project Path="src/Maviray.Blazor.Components.Pro.Grid.UI/Maviray.Blazor.Components.Pro.Grid.UI.csproj" Id="d5a8bbe4-63a8-41eb-ad9f-f659ada903bf" />
</Solution>
```

- [ ] **Step 3: Write the smoke test**

`src/Maviray.Blazor.Components.Pro.Core.Tests/SmokeTests.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Core.Constants;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void TestProject_ReferencesCoreAssembly()
    {
        Assert.Equal("Id", QueryConstants.ID);
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests`
Expected: `Passed! - Failed: 0, Passed: 1, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core.Tests Maviray.Blazor.Components.Pro.slnx
git commit -m "test: scaffold Core test project"
```

---

### Task 2: `GridPropertyPath` and `GridPropertyPathMap<TDataItem, TModel>`

The consumer-facing mapping API: a fluent map from a `TModel` property (selected via a strongly-typed expression) to a `TDataItem` dot-path, with an optional null-coalescing default.

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Core/Models/Grid/GridPropertyPath.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Core/Models/Grid/GridPropertyPathMap.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Core/AssemblyInfo.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestVendor.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestOrder.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestOrderGridModel.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Models/Grid/GridPropertyPathMapTests.cs`

**Interfaces:**
- Consumes: nothing new (uses only BCL `System.Linq.Expressions`/`System.Reflection`).
- Produces:
  - `internal sealed record GridPropertyPath(string DataItemPath, object? NullDefault)` in `Maviray.Blazor.Components.Pro.Core.Models.Grid`.
  - `public sealed class GridPropertyPathMap<TDataItem, TModel> where TDataItem : class` in `Maviray.Blazor.Components.Pro.Core.Models.Grid`, with:
    - `public GridPropertyPathMap<TDataItem, TModel> Map(Expression<Func<TModel, object?>> modelProperty, string dataItemPath, object? nullDefault = null)`
    - `internal bool TryResolve(string modelPropertyName, out GridPropertyPath path)`
  - Test fixtures `TestVendor`, `TestOrder` (`TDataItem`), `TestOrderGridModel` (`TModel`, implements `IGridCollectionModel<TestOrder, int>`) — reused by every later task's tests.
  - `[assembly: InternalsVisibleTo("Maviray.Blazor.Components.Pro.Core.Tests")]` on the Core assembly, needed because `TryResolve` and (Task 4's) `GridPropertyExpressionBuilder` are `internal`.

- [ ] **Step 1: Write the failing tests and fixtures**

`src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestVendor.cs`:

```csharp
namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public class TestVendor
{
    public string? Code { get; set; }
}
```

`src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestOrder.cs`:

```csharp
namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public class TestOrder
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool? Approved { get; set; }
    public TestVendor? Vendor { get; set; }
}
```

`src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestOrderGridModel.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

// Mirrors the shape of a real-world custom-constructed grid model (e.g. PurchaseOrderItemGridModel):
// Id/Name are 1:1 with TestOrder, VendorCode is reachable via a TestOrder navigation path,
// DisplayLabel is irreducibly computed, and ApprovedOrDefault mirrors a "?? false" coalesce.
public class TestOrderGridModel : IGridCollectionModel<TestOrder, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2)]
    public string? Name { get; set; }

    [GridDataProperty(3)]
    public string? VendorCode { get; set; }

    [GridDataProperty(4)]
    public string? DisplayLabel { get; set; }

    [GridDataProperty(5)]
    public bool ApprovedOrDefault { get; set; }

    public int Key => Id;

    public void ConstructFrom(TestOrder dataItem)
    {
        Id = dataItem.Id;
        Name = dataItem.Name;
        VendorCode = dataItem.Vendor?.Code;
        DisplayLabel = $"{dataItem.Id}-{dataItem.Name}";
        ApprovedOrDefault = dataItem.Approved ?? false;
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}
```

`src/Maviray.Blazor.Components.Pro.Core.Tests/Models/Grid/GridPropertyPathMapTests.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Models.Grid;

public class GridPropertyPathMapTests
{
    [Fact]
    public void Map_ThenTryResolve_ReturnsRegisteredPath()
    {
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.VendorCode, "Vendor.Code");

        var resolved = map.TryResolve("VendorCode", out var path);

        Assert.True(resolved);
        Assert.Equal("Vendor.Code", path.DataItemPath);
        Assert.Null(path.NullDefault);
    }

    [Fact]
    public void Map_WithNullDefault_IsCarriedThroughToResolvedPath()
    {
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.ApprovedOrDefault, "Approved", nullDefault: false);

        map.TryResolve("ApprovedOrDefault", out var path);

        Assert.Equal("Approved", path.DataItemPath);
        Assert.Equal(false, path.NullDefault);
    }

    [Fact]
    public void TryResolve_UnregisteredProperty_ReturnsFalse()
    {
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>();

        var resolved = map.TryResolve("Name", out _);

        Assert.False(resolved);
    }

    [Fact]
    public void Map_BoxedValueTypeProperty_ExtractsPropertyNameFromConvertExpression()
    {
        // ApprovedOrDefault is a bool, so the Expression<Func<TModel, object?>> selector wraps the
        // property access in a Convert node - this guards against GetPropertyName only handling
        // reference-typed properties.
        var map = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.ApprovedOrDefault, "Approved");

        var resolved = map.TryResolve("ApprovedOrDefault", out var path);

        Assert.True(resolved);
        Assert.Equal("Approved", path.DataItemPath);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter GridPropertyPathMapTests`
Expected: build error — `GridPropertyPathMap<,>` does not exist yet.

- [ ] **Step 3: Implement**

`src/Maviray.Blazor.Components.Pro.Core/Models/Grid/GridPropertyPath.cs`:

```csharp
namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// A resolved <typeparamref name="TDataItem"/>-side sort/filter target for a <c>TModel</c> property:
/// the dot-path to walk, and an optional value a <see langword="null"/> at that path should be
/// coalesced to before comparing/ordering. See <see cref="GridPropertyPathMap{TDataItem,TModel}"/>.
/// </summary>
internal sealed record GridPropertyPath(string DataItemPath, object? NullDefault);
```

`src/Maviray.Blazor.Components.Pro.Core/Models/Grid/GridPropertyPathMap.cs`:

```csharp
using System.Linq.Expressions;
using System.Reflection;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

/// <summary>
/// Maps a <typeparamref name="TModel"/> property built from custom logic in
/// <c>IGridCollectionModel&lt;TDataItem,TKey&gt;.ConstructFrom</c> to the <typeparamref name="TDataItem"/>
/// dot-path <see cref="Services.GenericGridDataFactory{TDataItem,TModel,TKey}"/> should use to sort/filter
/// by that property. Optionally carries a <c>nullDefault</c> so a nullable navigation value that
/// <c>ConstructFrom</c> coalesces (e.g. <c>dataItem.Nav?.Flag ?? false</c>) sorts/filters the same way at
/// the database as it does in memory.
/// </summary>
public sealed class GridPropertyPathMap<TDataItem, TModel>
    where TDataItem : class
{
    private readonly Dictionary<string, GridPropertyPath> _paths = [];

    public GridPropertyPathMap<TDataItem, TModel> Map(
        Expression<Func<TModel, object?>> modelProperty,
        string dataItemPath,
        object? nullDefault = null)
    {
        _paths[GetPropertyName(modelProperty)] = new GridPropertyPath(dataItemPath, nullDefault);
        return this;
    }

    internal bool TryResolve(string modelPropertyName, out GridPropertyPath path) =>
        _paths.TryGetValue(modelPropertyName, out path!);

    private static string GetPropertyName(Expression<Func<TModel, object?>> modelProperty)
    {
        var body = modelProperty.Body;

        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression { Member: PropertyInfo property })
        {
            return property.Name;
        }

        throw new ArgumentException($"Expression '{modelProperty}' must be a simple property access.", nameof(modelProperty));
    }
}
```

`src/Maviray.Blazor.Components.Pro.Core/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Maviray.Blazor.Components.Pro.Core.Tests")]
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter GridPropertyPathMapTests`
Expected: `Passed! - Failed: 0, Passed: 4, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core/Models/Grid/GridPropertyPath.cs src/Maviray.Blazor.Components.Pro.Core/Models/Grid/GridPropertyPathMap.cs src/Maviray.Blazor.Components.Pro.Core/AssemblyInfo.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures src/Maviray.Blazor.Components.Pro.Core.Tests/Models
git commit -m "feat: add GridPropertyPathMap for custom-constructed grid model properties"
```

---

### Task 3: `Sortable`/`Filterable` flags on `MaviGridColumn`

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Models/Grid/MaviGridColumn.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Models/Grid/MaviGridColumnTests.cs`

**Interfaces:**
- Produces: `MaviGridColumn.Sortable` (`bool`, default `true`), `MaviGridColumn.Filterable` (`bool`, default `true`). Task 8 sets these to `false` when a property is unresolvable; Task 9's UI reads them to gate sort/filter affordances.

- [ ] **Step 1: Write the failing test**

`src/Maviray.Blazor.Components.Pro.Core.Tests/Models/Grid/MaviGridColumnTests.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Models.Grid;

public class MaviGridColumnTests
{
    [Fact]
    public void NewColumn_DefaultsToSortableAndFilterable()
    {
        var column = new MaviGridColumn();

        Assert.True(column.Sortable);
        Assert.True(column.Filterable);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter MaviGridColumnTests`
Expected: build error — `MaviGridColumn` has no member `Sortable`/`Filterable`.

- [ ] **Step 3: Implement**

Replace the full contents of `src/Maviray.Blazor.Components.Pro.Core/Models/Grid/MaviGridColumn.cs` with:

```csharp
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;

namespace Maviray.Blazor.Components.Pro.Core.Models.Grid;

public class MaviGridColumn
{
    public string Guid { get; init; } = System.Guid.NewGuid().ToString();
    public string? Title { get; set; }
    public string Key { get; set; } = System.Guid.NewGuid().ToString(); // data item property name
    public bool Active { get; set; }
    public bool Visible { get; set; } = true;
    public int Sequence { get; set; }
    public SortOrder SortOrder { get; set; }
    public MaviGridColumnCellBehavior MaviGridColumnBehavior { get; set; }
    public MaviGridColumnType GridColumnType { get; set; }
    public MaviGridPropertyType GridPropertyType { get; set; }
    public MaviGridFilterCriteria CurrentFilter { get; set; }
    public HorizontalPosition HorizontalTextAlignment { get; set; }
    public object? CurrentFilerValue { get; set; }
    public string? BoolPositiveDisplay { get; set; } // displays value when column is of boolean type and value is true/selected
    public string? BoolNegativeDisplay { get; set; } // displays value when column is of boolean type and value is false/unselected
    public string? DateTimeFormat { get; set; } // custom format applied to DateTime/DateTimeOffset cells; null uses the default

    // When true, the column's cells should be rendered with an advanced/graphical presentation
    // (e.g. a badge) instead of plain text. Driven by GridAdvancedDisplayAttribute on the model property.
    public bool GraphicDisplay { get; set; }

    // When true, the column's header gets a clickable sort affordance. False when the property could
    // not be resolved to a TDataItem path (see GridPropertyPathMap) - set once per column at build time.
    public bool Sortable { get; set; } = true;

    // When true, the column renders a filter input. False when the property could not be resolved to
    // a TDataItem path (see GridPropertyPathMap) - set once per column at build time.
    public bool Filterable { get; set; } = true;

    public Dictionary<string, string>? PossibleFilterValues { get; set; } = [];
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter MaviGridColumnTests`
Expected: `Passed! - Failed: 0, Passed: 1, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core/Models/Grid/MaviGridColumn.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Models/Grid/MaviGridColumnTests.cs
git commit -m "feat: add Sortable/Filterable flags to MaviGridColumn"
```

---

### Task 4: `GridPropertyExpressionBuilder` (shared coalesce-aware property expression)

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Core/Services/GridPropertyExpressionBuilder.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GridPropertyExpressionBuilderTests.cs`

**Interfaces:**
- Consumes: `TestOrder`/`TestVendor` fixtures from Task 2.
- Produces: `internal static class GridPropertyExpressionBuilder` with `public static Expression BuildPropertyExpression(Expression parameter, string propertyPath, object? nullDefault)` in `Maviray.Blazor.Components.Pro.Core.Services`. Tasks 5 and 6 call this instead of their own inline dot-path-walking loops.

- [ ] **Step 1: Write the failing tests**

`src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GridPropertyExpressionBuilderTests.cs`:

```csharp
using System.Linq.Expressions;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GridPropertyExpressionBuilderTests
{
    [Fact]
    public void BuildPropertyExpression_SimpleProperty_ReturnsMemberAccess()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        var property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "Name", nullDefault: null);

        var compiled = Expression.Lambda<Func<TestOrder, string?>>(property, parameter).Compile();
        Assert.Equal("Acme", compiled(new TestOrder { Name = "Acme" }));
    }

    [Fact]
    public void BuildPropertyExpression_NestedPath_WalksNavigation()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        var property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "Vendor.Code", nullDefault: null);

        var compiled = Expression.Lambda<Func<TestOrder, string?>>(property, parameter).Compile();
        Assert.Equal("V1", compiled(new TestOrder { Vendor = new TestVendor { Code = "V1" } }));
    }

    [Fact]
    public void BuildPropertyExpression_NullableWithNullDefault_CoalescesNullToDefault()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        var property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "Approved", nullDefault: false);

        var compiled = Expression.Lambda<Func<TestOrder, bool>>(property, parameter).Compile();

        Assert.False(compiled(new TestOrder { Approved = null }));
        Assert.False(compiled(new TestOrder { Approved = false }));
        Assert.True(compiled(new TestOrder { Approved = true }));
    }

    [Fact]
    public void BuildPropertyExpression_UnknownProperty_Throws()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        Assert.ThrowsAny<Exception>(() =>
            GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "DoesNotExist", nullDefault: null));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter GridPropertyExpressionBuilderTests`
Expected: build error — `GridPropertyExpressionBuilder` does not exist yet.

- [ ] **Step 3: Implement**

`src/Maviray.Blazor.Components.Pro.Core/Services/GridPropertyExpressionBuilder.cs`:

```csharp
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Services;

/// <summary>
/// Builds the property-access expression shared by <see cref="Extensions.QueryableExtensions.OrderByDynamic{TEntity}"/>
/// and <see cref="QueryBuilder{TDataItem,TKey}.BuildFilterExpression"/>: walks a dotted property path off
/// <paramref name="parameter"/>, then - only when <paramref name="nullDefault"/> is supplied - coalesces a
/// <see langword="null"/> at that path to the given default, so a nullable navigation value sorts/filters
/// at the data source the same way a model's <c>ConstructFrom</c> coalesces it in memory (e.g. <c>?? false</c>).
/// </summary>
internal static class GridPropertyExpressionBuilder
{
    public static Expression BuildPropertyExpression(Expression parameter, string propertyPath, object? nullDefault)
    {
        Expression property = propertyPath.Split('.').Aggregate(parameter, Expression.PropertyOrField);

        if (nullDefault == null)
        {
            return property;
        }

        var underlyingType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
        return Expression.Coalesce(property, Expression.Constant(nullDefault, underlyingType));
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter GridPropertyExpressionBuilderTests`
Expected: `Passed! - Failed: 0, Passed: 4, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core/Services/GridPropertyExpressionBuilder.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GridPropertyExpressionBuilderTests.cs
git commit -m "feat: add shared coalesce-aware property expression builder"
```

---

### Task 5: `QueryableExtensions.OrderByDynamic` — coalesce support

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Extensions/QueryableExtensions.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Extensions/QueryableExtensionsTests.cs`

**Interfaces:**
- Consumes: `GridPropertyExpressionBuilder.BuildPropertyExpression` (Task 4), `TestOrder`/`TestVendor` fixtures (Task 2).
- Produces: `OrderByDynamic<TEntity>(this IQueryable<TEntity> source, string propertyName, SortOrder sortOrder, object? nullDefault = null)` — same name, same first three parameters as today, one new optional trailing parameter. Task 7's `EntityFrameworkGridDataSource`/`FakeGridDataSource` call this with the new parameter.

- [ ] **Step 1: Write the failing tests**

`src/Maviray.Blazor.Components.Pro.Core.Tests/Extensions/QueryableExtensionsTests.cs`:

```csharp
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Extensions;

public class QueryableExtensionsTests
{
    [Fact]
    public void OrderByDynamic_WithNullDefaultFalse_TreatsNullAsFalse()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Approved = true },
            new() { Id = 2, Approved = null },
            new() { Id = 3, Approved = false }
        }.AsQueryable();

        var ordered = items.OrderByDynamic("Approved", SortOrder.Ascending, nullDefault: false).Select(x => x.Id).ToList();

        // Both the null row (Id 2) and the explicit false row (Id 3) coalesce to false and sort
        // before the true row (Id 1, always last); their relative order is whichever stable sort picks.
        Assert.Equal(1, ordered[2]);
        Assert.Contains(2, ordered.Take(2));
        Assert.Contains(3, ordered.Take(2));
    }

    [Fact]
    public void OrderByDynamic_NoNullDefault_MatchesTodaysBehavior()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Name = "B" },
            new() { Id = 2, Name = "A" }
        }.AsQueryable();

        var ordered = items.OrderByDynamic("Name", SortOrder.Ascending).Select(x => x.Id).ToList();

        Assert.Equal([2, 1], ordered);
    }

    [Fact]
    public void OrderByDynamic_NestedPath_SortsByNavigationProperty()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Vendor = new TestVendor { Code = "B" } },
            new() { Id = 2, Vendor = new TestVendor { Code = "A" } }
        }.AsQueryable();

        var ordered = items.OrderByDynamic("Vendor.Code", SortOrder.Ascending).Select(x => x.Id).ToList();

        Assert.Equal([2, 1], ordered);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter QueryableExtensionsTests`
Expected: `OrderByDynamic_WithNullDefaultFalse_TreatsNullAsFalse` fails to build (no `nullDefault` parameter overload yet); the other two currently pass against the unmodified method (that's fine — they're the regression baseline).

- [ ] **Step 3: Implement**

Modify `src/Maviray.Blazor.Components.Pro.Core/Extensions/QueryableExtensions.cs` — replace the method body:

```csharp
    public static IQueryable<TEntity> OrderByDynamic<TEntity>(this IQueryable<TEntity> source, string propertyName, SortOrder sortOrder, object? nullDefault = null)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        }

        // Parameter for the lambda: x => x.Property
        var parameter = Expression.Parameter(typeof(TEntity), QueryConstants.PARAMETER);

        // Support nested properties; when nullDefault is supplied the final access is coalesced so a
        // NULL at the data source sorts the same as the in-memory default the model's ConstructFrom uses.
        Expression property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, propertyName, nullDefault);

        // Get the property type dynamically
        var propertyType = property.Type;

        // Build the lambda expression: x => x.Property
        var lambda = Expression.Lambda(property, parameter);

        // Determine the OrderBy or OrderByDescending method
        var methodName = sortOrder == SortOrder.Ascending ? QueryConstants.ORDER_BY : QueryConstants.ORDER_BY_DESCENDING;

        // Find the generic OrderBy method
        var method = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), propertyType);

        // Apply the method dynamically to the source query
        var invokedMethod = method.Invoke(null, [source, lambda]);

        if (invokedMethod == null)
        {
            return source;
        }

        var resultingQuery = (IQueryable<TEntity>)invokedMethod;

        return resultingQuery;
    }
```

Add `using Maviray.Blazor.Components.Pro.Core.Services;` to the file's using block (needed for `GridPropertyExpressionBuilder`).

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter QueryableExtensionsTests`
Expected: `Passed! - Failed: 0, Passed: 3, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core/Extensions/QueryableExtensions.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Extensions/QueryableExtensionsTests.cs
git commit -m "feat: support null-coalescing default in OrderByDynamic"
```

---

### Task 6: `QueryBuilder.BuildFilterExpression` — coalesce support

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Services/QueryBuilder.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Services/QueryBuilderTests.cs`

**Interfaces:**
- Consumes: `GridPropertyExpressionBuilder.BuildPropertyExpression` (Task 4), `TestOrder`/`TestVendor` fixtures (Task 2).
- Produces: `QueryBuilder<TDataItem, TKey>.BuildFilterExpression(string propertyPath, MaviGridFilterCriteria gridFilterCriteria, object value, object? nullDefault = null)` — same name/first three parameters, one new optional trailing parameter. Task 8's `GenericGridDataFactory.ConstructFilter` calls this with the resolved `GridPropertyPath.NullDefault`.

- [ ] **Step 1: Write the failing tests**

`src/Maviray.Blazor.Components.Pro.Core.Tests/Services/QueryBuilderTests.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class QueryBuilderTests
{
    [Fact]
    public void BuildFilterExpression_NoNullDefault_ExcludesNullRowsFromIsFalse()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Approved = false },
            new() { Id = 2, Approved = null },
            new() { Id = 3, Approved = true }
        }.AsQueryable();

        var filter = QueryBuilder<TestOrder, int>.BuildFilterExpression("Approved", MaviGridFilterCriteria.IsFalse, false);

        var matched = items.Where(filter).Select(x => x.Id).ToList();

        Assert.Equal([1], matched);
    }

    [Fact]
    public void BuildFilterExpression_WithNullDefaultFalse_IsFalseMatchesNullAndFalse()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Approved = false },
            new() { Id = 2, Approved = null },
            new() { Id = 3, Approved = true }
        }.AsQueryable();

        var filter = QueryBuilder<TestOrder, int>.BuildFilterExpression("Approved", MaviGridFilterCriteria.IsFalse, false, nullDefault: false);

        var matched = items.Where(filter).Select(x => x.Id).ToList();

        Assert.Equal([1, 2], matched);
    }

    [Fact]
    public void BuildFilterExpression_NestedPath_FiltersByNavigationProperty()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Vendor = new TestVendor { Code = "A" } },
            new() { Id = 2, Vendor = new TestVendor { Code = "B" } }
        }.AsQueryable();

        var filter = QueryBuilder<TestOrder, int>.BuildFilterExpression("Vendor.Code", MaviGridFilterCriteria.Equals, "B");

        var matched = items.Where(filter).Select(x => x.Id).ToList();

        Assert.Equal([2], matched);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter QueryBuilderTests`
Expected: `BuildFilterExpression_WithNullDefaultFalse_IsFalseMatchesNullAndFalse` fails to build (no `nullDefault` overload yet); the other two currently pass — regression baseline.

- [ ] **Step 3: Implement**

Modify `src/Maviray.Blazor.Components.Pro.Core/Services/QueryBuilder.cs`:

```csharp
    public static Expression<Func<TDataItem, bool>> BuildFilterExpression(string propertyPath, MaviGridFilterCriteria gridFilterCriteria, object value, object? nullDefault = null)
    {
        var parameter = Expression.Parameter(typeof(TDataItem), QueryConstants.PARAMETER);

        // Handle nested properties; when nullDefault is supplied the final access is coalesced so a
        // NULL at the data source is treated as the same default value ConstructFrom uses in memory.
        var property = GetPropertyExpression(parameter, propertyPath, nullDefault);

        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyPath}' not found.");
        }
```

(Rest of the method body, from `// Build condition based on the filter operator` onward, is unchanged.)

```csharp
    private static Expression? GetPropertyExpression(Expression parameter, string propertyPath, object? nullDefault)
    {
        return GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, propertyPath, nullDefault);
    }
```

(Replaces the old `GetPropertyExpression(Expression parameter, string propertyPath)` overload — same file, same namespace as `GridPropertyExpressionBuilder`, no new `using` needed.)

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter QueryBuilderTests`
Expected: `Passed! - Failed: 0, Passed: 3, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core/Services/QueryBuilder.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Services/QueryBuilderTests.cs
git commit -m "feat: support null-coalescing default in QueryBuilder filter expressions"
```

---

### Task 7: Thread `orderByNullDefault` through `IGridDataSource<TDataItem>`

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Interfaces/Grid/IGridDataSource.cs`
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.EntityFramework/Services/EntityFrameworkGridDataSource.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/FakeGridDataSource.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/FakeGridDataSourceTests.cs`

**Interfaces:**
- Consumes: `OrderByDynamic` with `nullDefault` (Task 5).
- Produces: `IGridDataSource<TDataItem>.Get(..., GridIncludeConfigurator<TDataItem>? includeConfigurator = null, object? orderByNullDefault = null)` — one new optional trailing parameter (same position in both the interface and `EntityFrameworkGridDataSource`). `FakeGridDataSource` — an in-memory `IGridDataSource<TestOrder>` implementation Task 8's factory tests construct `GenericGridDataFactory<TestOrder, TestOrderGridModel, int>` with.

There's no EF Core test infrastructure in this repo, and adding one (e.g. the EF Core InMemory provider) just to cover a single pass-through parameter is disproportionate. `EntityFrameworkGridDataSource`'s change is verified by the solution building (Step 4) and is structurally identical to the change already proven correct by `FakeGridDataSource`'s test in this task — both do nothing but forward `orderByNullDefault` into `OrderByDynamic`.

- [ ] **Step 1: Write the failing test and fixture**

`src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/FakeGridDataSource.cs`:

```csharp
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public class FakeGridDataSource(IReadOnlyList<TestOrder> items) : IGridDataSource<TestOrder>
{
    public Task<int> Count(Expression<Func<TestOrder, bool>>? filter = null)
    {
        IQueryable<TestOrder> query = items.AsQueryable();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        return Task.FromResult(query.Count());
    }

    public Task<IQueryable<TestOrder>?> Get(
        SortOrder sortOrder,
        string orderBy,
        int skip,
        int numberOfItemsToTake,
        Expression<Func<TestOrder, bool>>? filter = null,
        IReadOnlyCollection<string>? includePaths = null,
        GridIncludeConfigurator<TestOrder>? includeConfigurator = null,
        object? orderByNullDefault = null)
    {
        IQueryable<TestOrder> query = items.AsQueryable();

        if (includeConfigurator != null)
        {
            query = includeConfigurator(query);
        }

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var ordered = query.OrderByDynamic(orderBy, sortOrder, orderByNullDefault).Skip(skip).Take(numberOfItemsToTake);

        return Task.FromResult<IQueryable<TestOrder>?>(ordered);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

`src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/FakeGridDataSourceTests.cs`:

```csharp
using Maviray.Blazor.Components.Core.Enums;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public class FakeGridDataSourceTests
{
    [Fact]
    public async Task Get_ThreadsOrderByNullDefault_IntoCoalescedOrdering()
    {
        var source = new FakeGridDataSource(
        [
            new TestOrder { Id = 1, Approved = true },
            new TestOrder { Id = 2, Approved = null },
            new TestOrder { Id = 3, Approved = false }
        ]);

        var result = await source.Get(SortOrder.Ascending, "Approved", 0, 10, orderByNullDefault: false);

        var ids = result!.Select(x => x.Id).ToList();

        Assert.Equal(1, ids[2]);
    }

    [Fact]
    public async Task Count_WithFilter_ReturnsMatchingCount()
    {
        var source = new FakeGridDataSource(
        [
            new TestOrder { Id = 1, Name = "A" },
            new TestOrder { Id = 2, Name = "B" }
        ]);

        var count = await source.Count(x => x.Name == "A");

        Assert.Equal(1, count);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter FakeGridDataSourceTests`
Expected: build error — `IGridDataSource<TestOrder>.Get` has no `orderByNullDefault` parameter yet, so `FakeGridDataSource` doesn't satisfy the interface.

- [ ] **Step 3: Implement**

Modify `src/Maviray.Blazor.Components.Pro.Core/Interfaces/Grid/IGridDataSource.cs`:

```csharp
using Maviray.Blazor.Components.Core.Enums;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

public interface IGridDataSource<TDataItem> : IAsyncDisposable where TDataItem : class
{
    Task<int> Count(Expression<Func<TDataItem, bool>>? filter = null);

    /// <param name="includePaths">
    /// Names of navigation properties on <typeparamref name="TDataItem"/> to eager-load so that the
    /// related data is available when projecting nested grid columns. Paths may be dotted for deeper
    /// navigations (e.g. "Order.Customer").
    /// </param>
    /// <param name="includeConfigurator">
    /// An optional, strongly-typed query shaper applied to the root query before filtering, ordering and
    /// paging. It is intended for eager-loading related entities (e.g. EF Core
    /// <c>Include(...).ThenInclude(...)</c>) that cannot be expressed as the dotted string paths in
    /// <paramref name="includePaths"/>, so the materialized graph is available when each model is built.
    /// When both are supplied the configurator is applied in addition to <paramref name="includePaths"/>.
    /// </param>
    /// <param name="orderByNullDefault">
    /// When supplied, a <see langword="null"/> value at <paramref name="orderBy"/>'s resolved path is
    /// coalesced to this default before ordering, so sorting matches whatever default the model's
    /// <c>ConstructFrom</c> applies in memory (e.g. <c>?? false</c>).
    /// </param>
    Task<IQueryable<TDataItem>?> Get(SortOrder sortOrder, string orderBy, int skip, int numberOfItemsToTake, Expression<Func<TDataItem, bool>>? filter = null, IReadOnlyCollection<string>? includePaths = null, GridIncludeConfigurator<TDataItem>? includeConfigurator = null, object? orderByNullDefault = null);
}
```

Modify `src/Maviray.Blazor.Components.Pro.Grid.EntityFramework/Services/EntityFrameworkGridDataSource.cs` — update the method signature and the final line:

```csharp
    public async Task<IQueryable<TDataItem>?> Get(SortOrder sortOrder, string orderBy, int skip, int numberOfItemsToTake, Expression<Func<TDataItem, bool>>? filter = null, IReadOnlyCollection<string>? includePaths = null, GridIncludeConfigurator<TDataItem>? includeConfigurator = null, object? orderByNullDefault = null)
    {
```

(signature line only changes; body is unchanged until the final `return` statement)

```csharp
        return query.OrderByDynamic(orderBy, sortOrder, orderByNullDefault).Skip(skip).Take(numberOfItemsToTake);
    }
```

- [ ] **Step 4: Run the tests to verify they pass, and confirm the solution builds**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter FakeGridDataSourceTests`
Expected: `Passed! - Failed: 0, Passed: 2, Skipped: 0`

Run: `dotnet build Maviray.Blazor.Components.Pro.slnx`
Expected: `Build succeeded.` (confirms `EntityFrameworkGridDataSource` still satisfies `IGridDataSource<TDataItem>`)

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core/Interfaces/Grid/IGridDataSource.cs src/Maviray.Blazor.Components.Pro.Grid.EntityFramework/Services/EntityFrameworkGridDataSource.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/FakeGridDataSource.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/FakeGridDataSourceTests.cs
git commit -m "feat: thread orderByNullDefault through IGridDataSource"
```

---

### Task 8: `GenericGridDataFactory` resolution + wiring

The core task: resolve every sort key, filter key, and column through the map, falling back to identical-name, and never throwing.

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Interfaces/Grid/IGenericGridDataFactory.cs`
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Services/GenericGridDataFactory.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GenericGridDataFactoryTests.cs`

**Interfaces:**
- Consumes: `GridPropertyPathMap<TDataItem, TModel>`/`GridPropertyPath` (Task 2), `MaviGridColumn.Sortable`/`Filterable` (Task 3), `QueryBuilder.BuildFilterExpression` with `nullDefault` (Task 6), `IGridDataSource<TDataItem>.Get` with `orderByNullDefault` (Task 7), `FakeGridDataSource`/`TestOrder`/`TestOrderGridModel` fixtures (Tasks 2, 7).
- Produces: `BuildDataCollection(..., GridPropertyPathMap<TDataItem, TModel>? propertyPathMap = null)` — one new optional trailing parameter on both `IGenericGridDataFactory<TDataItem,TModel,TKey>` and `GenericGridDataFactory<TDataItem,TModel,TKey>`. Task 9's `MaviGrid.razor` consumes the `Sortable`/`Filterable` flags this task sets on `MaviGridColumn`.

- [ ] **Step 1: Write the failing tests**

`src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GenericGridDataFactoryTests.cs`:

```csharp
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Enums.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;
using Maviray.Blazor.Components.Pro.Core.Services;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Services;

public class GenericGridDataFactoryTests
{
    private static GenericGridDataFactory<TestOrder, TestOrderGridModel, int> CreateFactory(IReadOnlyList<TestOrder> items) =>
        new(
            NullLogger<GenericGridDataFactory<TestOrder, TestOrderGridModel, int>>.Instance,
            new FakeGridDataSource(items),
            []);

    private static List<TestOrder> SampleOrders() =>
    [
        new() { Id = 1, Name = "Order 1", Approved = true, Vendor = new TestVendor { Code = "B" } },
        new() { Id = 2, Name = "Order 2", Approved = null, Vendor = new TestVendor { Code = "A" } },
        new() { Id = 3, Name = "Order 3", Approved = false, Vendor = new TestVendor { Code = "C" } }
    ];

    [Fact]
    public async Task BuildDataCollection_NoMapSupplied_SortsByIdenticallyNamedProperty_Unchanged()
    {
        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters { OrderByColumnKey = "Name", SortOrder = SortOrder.Ascending };

        var result = await factory.BuildDataCollection(parameters);

        Assert.NotNull(result);
        Assert.Equal(
            ["Order 1", "Order 2", "Order 3"],
            result!.Rows.Select(r => r.Cells.First(c => c.ColumnKey == "Name").Value));
    }

    [Fact]
    public async Task BuildDataCollection_UnmappedCustomProperty_ColumnIsNotSortableOrFilterable()
    {
        var factory = CreateFactory(SampleOrders());

        var result = await factory.BuildDataCollection(new MaviGridDataQueryParameters());

        var displayLabelColumn = result!.Columns.Single(c => c.Key == "DisplayLabel");

        Assert.False(displayLabelColumn.Sortable);
        Assert.False(displayLabelColumn.Filterable);
    }

    [Fact]
    public async Task BuildDataCollection_UnmappedCustomProperty_RequestingSortDoesNotThrow_FallsBackToId()
    {
        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters { OrderByColumnKey = "DisplayLabel", SortOrder = SortOrder.Ascending };

        var result = await factory.BuildDataCollection(parameters);

        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result!.Rows.Select(r => r.Key!.Value));
    }

    [Fact]
    public async Task BuildDataCollection_MappedNavigationProperty_IsSortableAndSortsByThePath()
    {
        var propertyPathMap = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.VendorCode, "Vendor.Code");

        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters { OrderByColumnKey = "VendorCode", SortOrder = SortOrder.Ascending };

        var result = await factory.BuildDataCollection(parameters, propertyPathMap: propertyPathMap);

        var vendorCodeColumn = result!.Columns.Single(c => c.Key == "VendorCode");
        Assert.True(vendorCodeColumn.Sortable);
        Assert.True(vendorCodeColumn.Filterable);

        // Vendor codes are A (Order 2), B (Order 1), C (Order 3).
        Assert.Equal([2, 1, 3], result.Rows.Select(r => r.Key!.Value));
    }

    [Fact]
    public async Task BuildDataCollection_MappedPropertyWithNullDefault_FilterMatchesCoalescedNullRow()
    {
        var propertyPathMap = new GridPropertyPathMap<TestOrder, TestOrderGridModel>()
            .Map(m => m.ApprovedOrDefault, "Approved", nullDefault: false);

        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters();
        parameters.AddFilter("ApprovedOrDefault", "false", MaviGridFilterCriteria.IsFalse);

        var result = await factory.BuildDataCollection(parameters, propertyPathMap: propertyPathMap);

        // Order 2 (Approved: null) and Order 3 (Approved: false) both coalesce to false.
        Assert.Equal([2, 3], result!.Rows.Select(r => r.Key!.Value));
    }

    [Fact]
    public async Task BuildDataCollection_UnresolvableFilterKey_DropsTermInsteadOfThrowing()
    {
        var factory = CreateFactory(SampleOrders());

        var parameters = new MaviGridDataQueryParameters();
        parameters.AddFilter("DisplayLabel", "anything", MaviGridFilterCriteria.Contains);

        var result = await factory.BuildDataCollection(parameters);

        Assert.NotNull(result);
        Assert.Equal(3, result!.TotalRows);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter GenericGridDataFactoryTests`
Expected:
- `BuildDataCollection_NoMapSupplied_SortsByIdenticallyNamedProperty_Unchanged` currently passes (baseline, unaffected by this change).
- `BuildDataCollection_UnmappedCustomProperty_ColumnIsNotSortableOrFilterable` fails — columns default `Sortable`/`Filterable` to `true` today.
- `BuildDataCollection_UnmappedCustomProperty_RequestingSortDoesNotThrow_FallsBackToId` fails — today this throws (`Expression.PropertyOrField` finds no `DisplayLabel` on `TestOrder`).
- The remaining three fail to build — no `propertyPathMap` parameter on `BuildDataCollection` yet.

- [ ] **Step 3: Implement**

Modify `src/Maviray.Blazor.Components.Pro.Core/Interfaces/Grid/IGenericGridDataFactory.cs` — add the doc comment and the new parameter to the method:

```csharp
using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;

public interface IGenericGridDataFactory<TDataItem, TModel, TKey> : IAsyncDisposable where TDataItem : class where TModel : class, IGridCollectionModel<TDataItem, TKey> where TKey : IEquatable<TKey>
{
    /// <param name="queryParameters"></param>
    /// <param name="additionalFilter">
    /// An optional consumer-supplied predicate (for example a permission or data-scope constraint)
    /// that is AND-combined with the filter derived from <paramref name="queryParameters"/>. When
    /// supplied it limits both the total count and the returned page to the matching data.
    /// </param>
    /// <param name="cellThemeResolvers">
    /// Optional per-column delegates, keyed by column key, that decide the <see cref="ThemeColorScheme"/>
    /// for each cell of a column marked for graphical display (<see cref="MaviGridColumn.GraphicDisplay"/>).
    /// A resolver receives the finalised cell together with the cell's raw, unformatted property value
    /// (so it can run typed logic such as numeric threshold comparisons) and returns the scheme an
    /// advanced renderer should use. Resolvers for columns that are not graphic, or columns without a
    /// resolver, are ignored.
    /// </param>
    /// <param name="includeConfigurator">
    /// An optional, strongly-typed query shaper applied to the root query before filtering, ordering and
    /// paging. Use it to eager-load related entities — typically via EF Core
    /// <c>Include(...).ThenInclude(...)</c> — that cannot be expressed through the model's
    /// <c>[GridNavProperty]</c> string paths, so the whole graph is materialized by the same paged query
    /// and is available when each <typeparamref name="TModel"/> is built in its
    /// <see cref="IGridCollectionModel{TDataItem,TKey}.ConstructFrom"/>.
    /// </param>
    /// <param name="propertyPathMap">
    /// Optional map from a <typeparamref name="TModel"/> property name (typically one built with custom
    /// logic in <see cref="IGridCollectionModel{TDataItem,TKey}.ConstructFrom"/> rather than a direct
    /// property match) to the <typeparamref name="TDataItem"/> dot-path to use when that property is
    /// sorted or filtered. A <typeparamref name="TModel"/> property with neither a map entry nor an
    /// identically-named <typeparamref name="TDataItem"/> member is marked
    /// <see cref="MaviGridColumn.Sortable"/>/<see cref="MaviGridColumn.Filterable"/> <see langword="false"/>
    /// instead of causing the request to fail.
    /// </param>
    Task<MaviGridCollection<TKey>?> BuildDataCollection(
        MaviGridDataQueryParameters queryParameters,
        Expression<Func<TDataItem, bool>>? additionalFilter = null,
        IReadOnlyDictionary<string, Func<MaviGridCell<TKey>, object?, ThemeColorScheme>>? cellThemeResolvers = null,
        GridIncludeConfigurator<TDataItem>? includeConfigurator = null,
        GridPropertyPathMap<TDataItem, TModel>? propertyPathMap = null);
}
```

Modify `src/Maviray.Blazor.Components.Pro.Core/Services/GenericGridDataFactory.cs`:

Replace `BuildDataCollection` (through `ConstructCollection`) with:

```csharp
    public async Task<MaviGridCollection<TKey>?> BuildDataCollection(
        MaviGridDataQueryParameters queryParameters,
        Expression<Func<TDataItem, bool>>? additionalFilter = null,
        IReadOnlyDictionary<string, Func<MaviGridCell<TKey>, object?, ThemeColorScheme>>? cellThemeResolvers = null,
        GridIncludeConfigurator<TDataItem>? includeConfigurator = null,
        GridPropertyPathMap<TDataItem, TModel>? propertyPathMap = null)
    {
        if (string.IsNullOrWhiteSpace(queryParameters.OrderByColumnKey))
        {
            queryParameters.OrderByColumnKey = QueryConstants.ID;
        }

        var resolvedOrderBy = ResolveDataItemPath(queryParameters.OrderByColumnKey, propertyPathMap)
            ?? new GridPropertyPath(QueryConstants.ID, null);

        Expression<Func<TDataItem, bool>>? filter = null;

        if (queryParameters.Filters != null && queryParameters.Filters.Any())
        {
            filter = ConstructFilter(queryParameters.Filters, propertyPathMap);
        }

        // Incorporate the consumer-supplied filter (e.g. permission or scope constraints) by
        // AND-combining it with the grid's own filter, so a user only ever sees data they are allowed
        // to and the same constraint applies to both the count and the page query.
        filter = CombineFilters(filter, additionalFilter);

        // Incorporate any globally registered, type-agnostic constraints (e.g. soft-delete or tenant
        // scoping) so they apply to every grid without per-page wiring.
        filter = CombineFilters(filter, BuildGlobalFilter());

        var totalItemCount = await _dataSource.Count(filter);

        var numberOfItemsToTake = ValidateNumberOfItemsToTake(queryParameters.NumberOfItemsPerPage);
        var totalPages = CalculateTotalPages(totalItemCount, numberOfItemsToTake);
        var requestedPageNumber = ValidateRequestedPage(totalPages, queryParameters.PageNumber);
        var skip = CalculateSkip(totalItemCount, requestedPageNumber, numberOfItemsToTake);


        IQueryable<TDataItem> ? resultingData = null;

        resultingData = await _dataSource.Get(queryParameters.SortOrder, resolvedOrderBy.DataItemPath, skip, numberOfItemsToTake, filter, NavigationIncludePaths, includeConfigurator, resolvedOrderBy.NullDefault);

        if (resultingData == null)
        {
            return null;
        }

        var collection = ConstructCollection(resultingData, cellThemeResolvers, propertyPathMap);

        if (collection.Columns.Count == 0)
        {
            return collection;
        }

        collection.Take = numberOfItemsToTake;
        collection.TotalPages = totalPages;
        collection.TotalRows = totalItemCount;
        collection.CurrentPage = requestedPageNumber;
        collection.SortOrder = queryParameters.SortOrder;
        collection.SortColumnKey = queryParameters.OrderByColumnKey;

        collection.FirstRowIndex = totalItemCount == 0 ? 0 : (collection.CurrentPage - 1) * collection.Take + 1;
        collection.LastRowIndex = totalItemCount == 0 ? 0 : Math.Min(collection.CurrentPage * collection.Take, collection.TotalRows);

        var activeColumn = collection.Columns.FirstOrDefault(x => x.Key.Equals(queryParameters.OrderByColumnKey)) ?? collection.Columns.FirstOrDefault();

        if (activeColumn != null)
        {
            activeColumn.Active = true;
            activeColumn.SortOrder = queryParameters.SortOrder;
        }

        if (queryParameters.Filters == null)
        {
            return collection;
        }

        {
            foreach (var requestedFilter in queryParameters.Filters)
            {
                var correspondingColumn = collection.Columns.FirstOrDefault(x => x.Key == requestedFilter.Key);

                if (correspondingColumn == null)
                {
                    continue;
                }

                correspondingColumn.CurrentFilerValue = requestedFilter.Value;
                correspondingColumn.CurrentFilter = requestedFilter.GridFilterCriteria;
            }
        }

        return collection;
    }

    public MaviGridCollection<TKey> ConstructCollection(
        IQueryable<TDataItem> queryable,
        IReadOnlyDictionary<string, Func<MaviGridCell<TKey>, object?, ThemeColorScheme>>? cellThemeResolvers = null,
        GridPropertyPathMap<TDataItem, TModel>? propertyPathMap = null)
    {
        var type = typeof(TModel);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var collection = new MaviGridCollection<TKey>();

        foreach (var dataItem in queryable)
        {
            var gridModelItem = dataItem.Adapt<TModel>(MappingConfig);
            gridModelItem.ConstructFrom(dataItem);

            var row = ConstructCollectionRow(gridModelItem, properties, collection.Columns, cellThemeResolvers, propertyPathMap);
            collection.Rows.Add(row);
        }

        return collection;
    }

    private static Expression<Func<TDataItem, bool>>? ConstructFilter(IEnumerable<GridFilterModel> filters, GridPropertyPathMap<TDataItem, TModel>? propertyPathMap)
    {
        Expression<Func<TDataItem, bool>>? expression = null;

        foreach (var item in filters)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || item.Value is null)
            {
                continue;
            }

            var resolvedPath = ResolveDataItemPath(item.Key, propertyPathMap);

            if (resolvedPath == null)
            {
                // No resolvable TDataItem path for this TModel property (neither an explicit map
                // entry nor an identically-named member) - drop the term instead of throwing.
                continue;
            }

            var filterExpression = QueryBuilder<TDataItem, TKey>.BuildFilterExpression(resolvedPath.DataItemPath, item.GridFilterCriteria, item.Value, resolvedPath.NullDefault);

            expression = expression == null ? filterExpression : QueryBuilder<TDataItem, TKey>.CombineExpressions(expression, filterExpression, MaviCombineExpressionCriteria.AndAlso);
        }

        return expression;
    }

    /// <summary>
    /// Resolves a <typeparamref name="TModel"/> property name to the <typeparamref name="TDataItem"/>
    /// dot-path the data source should sort/filter by: an explicit entry in
    /// <paramref name="propertyPathMap"/> takes precedence, then an identically-named member on
    /// <typeparamref name="TDataItem"/> (today's existing behavior), otherwise <see langword="null"/> -
    /// the property is not sortable/filterable.
    /// </summary>
    private static GridPropertyPath? ResolveDataItemPath(string modelPropertyName, GridPropertyPathMap<TDataItem, TModel>? propertyPathMap)
    {
        if (propertyPathMap != null && propertyPathMap.TryResolve(modelPropertyName, out var mappedPath))
        {
            return DataItemPathExists(mappedPath.DataItemPath) ? mappedPath : null;
        }

        return DataItemPathExists(modelPropertyName) ? new GridPropertyPath(modelPropertyName, null) : null;
    }

    /// <summary>
    /// Reflection-only existence check mirroring the member lookup <see cref="Expression.PropertyOrField(Expression, string)"/>
    /// performs, without throwing when a segment is missing.
    /// </summary>
    private static bool DataItemPathExists(string dotPath)
    {
        var currentType = typeof(TDataItem);

        foreach (var segment in dotPath.Split('.'))
        {
            MemberInfo? member = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
            member ??= currentType.GetField(segment, BindingFlags.Public | BindingFlags.Instance);

            if (member == null)
            {
                return false;
            }

            currentType = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
        }

        return true;
    }
```

Modify `ConstructCollectionRow`'s signature (add the new trailing parameter):

```csharp
    private static MaviGridRow<TKey> ConstructCollectionRow(
        TModel gridModel,
        PropertyInfo[] properties,
        List<MaviGridColumn> columns,
        IReadOnlyDictionary<string, Func<MaviGridCell<TKey>, object?, ThemeColorScheme>>? cellThemeResolvers,
        GridPropertyPathMap<TDataItem, TModel>? propertyPathMap)
```

Inside `ConstructCollectionRow`, in the `[GridDataProperty]` branch, replace the column-creation block:

```csharp
                var column = columns.FirstOrDefault(c => c.Key == prop.Name);

                if (column == null)
                {
                    var resolvedPath = ResolveDataItemPath(prop.Name, propertyPathMap);

                    column = new()
                    {
                        Key = prop.Name,
                        Title = DeterminePropertyTitle(prop),
                        GridPropertyType = GetPropertyType(prop.PropertyType),
                        Sequence = GetGridDataPropertySequence(prop),
                        MaviGridColumnBehavior = GetGridDataPropertyCellBehavior(prop),
                        GraphicDisplay = HasAdvancedDisplay(prop),
                        Sortable = resolvedPath != null,
                        Filterable = resolvedPath != null
                    };

                    ProcessColumn(column, prop);

                    columns.Add(column);
                }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter GenericGridDataFactoryTests`
Expected: `Passed! - Failed: 0, Passed: 6, Skipped: 0`

Run the full suite to confirm no regression: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests`
Expected: all tests from Tasks 1–8 pass.

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core/Interfaces/Grid/IGenericGridDataFactory.cs src/Maviray.Blazor.Components.Pro.Core/Services/GenericGridDataFactory.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GenericGridDataFactoryTests.cs
git commit -m "feat: resolve grid sort/filter through GridPropertyPathMap, falling back to not-sortable/not-filterable"
```

---

### Task 9: `MaviGrid.razor` — gate sort/filter UI on `Sortable`/`Filterable`

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor`

**Interfaces:**
- Consumes: `MaviGridColumn.Sortable`/`Filterable` (Task 3).
- Produces: no new public API — this is UI wiring only.

There's no bUnit (or other Blazor component test) infrastructure in this repo, and none of the Sample project's existing grid models exercise a `Sortable == false`/`Filterable == false` column (all their properties are still 1:1 today), so there's nothing new to render differently yet in the Sample app either. Verification for this task is: the solution builds, and the existing Sample app's grids (all-`Sortable`/all-`Filterable`, matching today) keep behaving exactly as before.

- [ ] **Step 1: Update the sort click handler**

Modify `src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor` — the header row's clickable div (originally lines 60–61):

```razor
                        <div class="@Tailwind.Theme.Typography.LABEL_LARGE select-none transition-colors flex items-center gap-2 @(column.Sortable ? $"cursor-pointer {Tailwind.Theme.Colors.Text.Hover.THEME_DEFAULT_TEN_TEXT_HOVER}" : "cursor-default opacity-70")"
                            @onclick="() => SortByColumn(column)">
```

(The `<span>@column.Title</span>` and sort-indicator `@if` block immediately below are unchanged.)

Modify the filter row (originally lines 116–125):

```razor
                @foreach (var column in GetVisibleColumns())
                {
                    <div class="px-4 py-2 bg-zinc-50">
                        @if (column.Filterable)
                        {
                            <input type="text"
                                   id="@($"filter-{column.Key}-{Id}")"
                                   class="@GetSearchInputCss()"
                                   placeholder="@Parameters?.FilterPlaceholderText"
                                   @oninput="(e) => UpdateColumnFilter(column.Key, e)"/>
                        }
                    </div>
                }
```

Modify `SortByColumn` (originally lines 621–646) to take the column and no-op when it isn't sortable:

```csharp
    protected async Task SortByColumn(MaviGridColumn column)
    {
        if (!column.Sortable)
        {
            return;
        }

        try
        {
            if (_queryParameters.OrderByColumnKey == column.Key)
            {
                _queryParameters.SortOrder = _queryParameters.SortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                _queryParameters.OrderByColumnKey = column.Key;
                _queryParameters.SortOrder = SortOrder.Ascending;
            }

            await RefreshTable();

            if (EnableLifeCycleLogging)
            {
                Logger?.LogDebugLifeCycle(Id, GetType());
            }
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, $"An error occurred while sorting by column with key '{column.Key}'.");
        }
    }
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/Maviray.Blazor.Components.Pro.Grid.UI`
Expected: `Build succeeded.`

Run: `dotnet build Maviray.Blazor.Components.Pro.slnx`
Expected: `Build succeeded.` (confirms `Maviray.Blazor.Components.Pro.Grid.Sample`, which consumes `MaviGrid`, still compiles against the changed `SortByColumn` signature)

- [ ] **Step 3: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Grid.UI/Components/MaviGrid.razor
git commit -m "feat: gate sort/filter UI affordances on MaviGridColumn.Sortable/Filterable"
```

---

## Post-plan verification

After Task 9, run the full test suite and full solution build once more before considering the plan complete:

```bash
dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests
dotnet build Maviray.Blazor.Components.Pro.slnx
```

Expected: all tests pass, solution builds with no warnings introduced by this change.

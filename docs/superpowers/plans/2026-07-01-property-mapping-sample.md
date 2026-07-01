# Property Mapping Sample Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix a real gap discovered in the already-shipped grid property-mapping feature (coalescing a non-nullable value type reached through an optional navigation throws), then add a real, running end-to-end demo of the fixed feature to `Maviray.Blazor.Components.Pro.Grid.Sample` — a 5-level entity chain, a flat grid model exercising all four resolution categories, and a new page.

**Architecture:** `GridPropertyExpressionBuilder`'s coalescing logic is corrected first, with regression tests in the existing `Maviray.Blazor.Components.Pro.Core.Tests` project. Then five new EF Core entities (`OrderItem → Order → Customer → Region → Country`, one nullable hop) are added to the existing `SampleDbContext`; one flat `OrderItemGridModel` whose properties fall into the four resolution categories the library feature supports; one new page (`PropertyMapping.razor`) wiring a `GridIncludeConfigurator` and a `GridPropertyPathMap` into `BuildDataCollection`, mirroring the real-world reference case that motivated the feature.

**Tech Stack:** .NET 10 / C#, EF Core (SQL Server), Blazor Server, Mapster (via the existing `GenericGridDataFactory`), xUnit.

**Design doc:** `docs/superpowers/specs/2026-07-01-property-mapping-sample-design.md`

## Global Constraints

- Target framework: `net10.0`. `Nullable`/`ImplicitUsings` stay `enable` (matches every existing project in this solution).
- Task 1 is library code (bug fix) — follows the same TDD/test discipline as the rest of `Maviray.Blazor.Components.Pro.Core`. Tasks 2-6 are sample/demo code — no new xUnit tests for those; verification is `dotnet build`. Task 7 is the final end-to-end run against a real database, matching the design doc's own testing plan.
- Do not modify `Home.razor`, `Scoped.razor`, or their existing models/entities/DbSets.
- No fluent `OnModelCreating` configuration needed — every new entity relies on EF Core naming conventions (`<Nav>Id` FK + `<Nav>` navigation), exactly like the existing `DbSampleItemTwo.DbSampleItemOneId`/`DbSampleItemOne` pair.
- `Customer.RegionId`/`Customer.Region` is the **only** nullable relationship in the new chain. Every other FK/navigation in the new chain is required (non-nullable).
- Every entity in the new chain must have all nine of: `string`, `char`, `bool`, `int`, `decimal`, an enum (`RatingEnum`), `DateTime`, `TimeSpan`, `Guid` typed properties, per the design doc's table.
- Every seeded `TimeSpan` value must stay under 24 hours. EF Core's SQL Server provider maps `TimeSpan` to the native `time` column type by default, whose valid range is `00:00:00` to `23:59:59.9999999` — a value of 24 hours or more throws at `SaveChangesAsync` time, not at build time.

---

### Task 1: Fix `GridPropertyExpressionBuilder` — coalesce a non-nullable value type reached through an optional navigation

**Background:** `GridPropertyExpressionBuilder.BuildPropertyExpression` coalesces a resolved property to a default when `nullDefault` is supplied, using `Expression.Coalesce`. `Expression.Coalesce` requires its left operand's type to be a reference type or `Nullable<T>`. The existing tests only exercise a property that is *itself* declared nullable (`TestOrder.Approved` is `bool?`). The feature's real motivating scenario — a property reached through an *optional navigation* whose own declared type is a plain, non-nullable value type (e.g. `Customer.Region?.Rating`, where `Region.Rating` is a non-nullable `RatingEnum`, or the real-world reference's `PurchaseOrder?.InvoiceReceived`, where `InvoiceReceived` is a plain `bool` on `PurchaseOrder`) — was never tested, and throws `InvalidOperationException` today, because `Expression.PropertyOrField` walking `"Region.Rating"` produces a `MemberExpression` whose `.Type` is the plain non-nullable enum, regardless of `Region` itself being optional.

**Files:**
- Modify: `src/Maviray.Blazor.Components.Pro.Core/Services/GridPropertyExpressionBuilder.cs`
- Modify: `src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestVendor.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GridPropertyExpressionBuilderTests.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Services/QueryBuilderTests.cs`
- Test: `src/Maviray.Blazor.Components.Pro.Core.Tests/Extensions/QueryableExtensionsTests.cs`

**Interfaces:**
- Consumes/modifies: `GridPropertyExpressionBuilder.BuildPropertyExpression(Expression parameter, string propertyPath, object? nullDefault)` — signature unchanged, only its internal coalescing logic changes.
- Produces: a fix relied on by every later task in this plan. `OrderItemGridModel`'s `RegionRatingOrDefault`/`IsInternationalOrDefault` mappings (Task 5) map exactly this shape — a non-nullable enum/bool on `Region`, reached through the optional `Customer.Region` navigation. Without this fix, Task 7's browser check would hit `InvalidOperationException` the first time `BuildDataCollection` resolves either of those two mapped properties.

Testing note: a *null* intermediate navigation (e.g. a customer with no region) only resolves safely at the database level via EF Core's SQL translation (LEFT JOIN) — there is no EF Core test infrastructure in this repo, so that part of the behavior cannot be unit-tested here. These tests prove the narrower, fully unit-testable claim: building and evaluating the coalesce expression against a *non-null* chain no longer throws and preserves the real value. The null-intermediate-navigation case is proven end-to-end, against a real database, by Task 7.

- [ ] **Step 1: Add a fixture property that reproduces the gap**

Replace the full contents of `src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestVendor.cs` with:

```csharp
namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public class TestVendor
{
    public string? Code { get; set; }
    public bool IsPreferred { get; set; }
}
```

- [ ] **Step 2: Write the failing tests**

Add this test method to the `GridPropertyExpressionBuilderTests` class in `src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GridPropertyExpressionBuilderTests.cs`:

```csharp
    [Fact]
    public void BuildPropertyExpression_NonNullableValueTypeWithNullDefault_DoesNotThrowAndPreservesValue()
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");

        // TestOrder.Vendor is a nullable reference (TestVendor?), but Vendor.IsPreferred is a
        // plain, non-nullable bool - mirrors a property reached through an optional EF Core
        // navigation whose own declared type is non-nullable (e.g. PurchaseOrder?.InvoiceReceived,
        // where InvoiceReceived is a plain bool on PurchaseOrder). Before the fix, this threw
        // InvalidOperationException from Expression.Coalesce ("used with a type that cannot be
        // null") because Vendor.IsPreferred's static type is bool, not bool?.
        var property = GridPropertyExpressionBuilder.BuildPropertyExpression(parameter, "Vendor.IsPreferred", nullDefault: false);

        var compiled = Expression.Lambda<Func<TestOrder, bool>>(property, parameter).Compile();

        Assert.True(compiled(new TestOrder { Vendor = new TestVendor { IsPreferred = true } }));
        Assert.False(compiled(new TestOrder { Vendor = new TestVendor { IsPreferred = false } }));
    }
```

Add this test method to the `QueryBuilderTests` class in `src/Maviray.Blazor.Components.Pro.Core.Tests/Services/QueryBuilderTests.cs`:

```csharp
    [Fact]
    public void BuildFilterExpression_NonNullableValueTypeThroughOptionalNavigation_WithNullDefault_DoesNotThrow()
    {
        // Only non-null Vendor rows here - proving a null intermediate navigation resolves safely
        // requires a real EF Core provider translating the LEFT JOIN, which this in-memory
        // LINQ-to-Objects suite cannot exercise. That end-to-end case is proven separately, against
        // a real database, by the "Custom Property Mapping" sample page (see the property-mapping
        // sample plan, Task 7).
        var items = new List<TestOrder>
        {
            new() { Id = 1, Vendor = new TestVendor { IsPreferred = true } },
            new() { Id = 2, Vendor = new TestVendor { IsPreferred = false } }
        }.AsQueryable();

        var filter = QueryBuilder<TestOrder, int>.BuildFilterExpression("Vendor.IsPreferred", MaviGridFilterCriteria.Equals, true, nullDefault: false);

        var matched = items.Where(filter).Select(x => x.Id).ToList();

        Assert.Equal([1], matched);
    }
```

Add this test method to the `QueryableExtensionsTests` class in `src/Maviray.Blazor.Components.Pro.Core.Tests/Extensions/QueryableExtensionsTests.cs`:

```csharp
    [Fact]
    public void OrderByDynamic_NonNullableValueTypeThroughOptionalNavigation_WithNullDefault_DoesNotThrow()
    {
        var items = new List<TestOrder>
        {
            new() { Id = 1, Vendor = new TestVendor { IsPreferred = false } },
            new() { Id = 2, Vendor = new TestVendor { IsPreferred = true } }
        }.AsQueryable();

        var ordered = items.OrderByDynamic("Vendor.IsPreferred", SortOrder.Ascending, nullDefault: false).Select(x => x.Id).ToList();

        Assert.Equal([1, 2], ordered);
    }
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter "GridPropertyExpressionBuilderTests|QueryBuilderTests|QueryableExtensionsTests"`
Expected: the three new tests fail with `System.InvalidOperationException` ("Coalesce used with type that cannot be null" or equivalent) — every other test in those three fixtures still passes.

- [ ] **Step 4: Fix `GridPropertyExpressionBuilder`**

Replace the full contents of `src/Maviray.Blazor.Components.Pro.Core/Services/GridPropertyExpressionBuilder.cs` with:

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

        // Expression.Coalesce requires its left operand to be a reference type or Nullable<T>. A
        // property reached through an optional navigation (e.g. Customer.Region?.Rating, where
        // Region is optional but Rating is a plain non-nullable enum declared on Region itself)
        // has a non-nullable static type even though the value can be missing at the database
        // level - convert to Nullable<T> first so Coalesce is valid to construct.
        Expression coalesceSource = property.Type.IsValueType && Nullable.GetUnderlyingType(property.Type) == null
            ? Expression.Convert(property, typeof(Nullable<>).MakeGenericType(property.Type))
            : property;

        return Expression.Coalesce(coalesceSource, Expression.Constant(nullDefault, underlyingType));
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests --filter "GridPropertyExpressionBuilderTests|QueryBuilderTests|QueryableExtensionsTests"`
Expected: all pass, including the three new tests.

- [ ] **Step 6: Run the full suite to confirm no regressions**

Run: `dotnet test src/Maviray.Blazor.Components.Pro.Core.Tests`
Expected: `Passed! - Failed: 0, Passed: 27, Skipped: 0` (24 pre-existing + 3 new).

- [ ] **Step 7: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Core/Services/GridPropertyExpressionBuilder.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Fixtures/TestVendor.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Services/GridPropertyExpressionBuilderTests.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Services/QueryBuilderTests.cs src/Maviray.Blazor.Components.Pro.Core.Tests/Extensions/QueryableExtensionsTests.cs
git commit -m "fix: coalesce non-nullable value types reached through optional navigations"
```

---

### Task 2: New entity chain, `RatingEnum`, and `SampleDbContext` wiring

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Enums/RatingEnum.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Country.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Region.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Customer.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Order.cs`
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/OrderItem.cs`
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Context/SampleDbContext.cs`

**Interfaces:**
- Produces: five EF Core entity classes (`Country`, `Region`, `Customer`, `Order`, `OrderItem`) and `RatingEnum { Low, Medium, High, Critical }`, all in `Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models`/`.Enums`. `SampleDbContext` exposes `DbSet<Country> Country`, `DbSet<Region> Region`, `DbSet<Customer> Customer`, `DbSet<Order> Order`, `DbSet<OrderItem> OrderItem`. Task 3 (migration), Task 4 (seeder), and Task 5 (grid model) all depend on these exact type/property names.

- [ ] **Step 1: Create the enum**

`src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Enums/RatingEnum.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;

public enum RatingEnum
{
    [Display(Name = "Low")]
    Low,

    [Display(Name = "Medium")]
    Medium,

    [Display(Name = "High")]
    High,

    [Display(Name = "Critical")]
    Critical
}
```

- [ ] **Step 2: Create `Country` (the deepest, leaf entity)**

`src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Country.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class Country
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Country Name")]
    public string? CountryName { get; set; }

    [Display(Name = "Country Grade")]
    public char CountryGrade { get; set; }

    [Display(Name = "Is Member State")]
    public bool IsMemberState { get; set; }

    [Display(Name = "Population (Millions)")]
    public int PopulationMillions { get; set; }

    [Display(Name = "GDP (Trillions)")]
    [Precision(18, 2)]
    public decimal GdpTrillion { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Independence Date")]
    public DateTime IndependenceDate { get; set; }

    [Display(Name = "Average Transit Time")]
    public TimeSpan AverageTransitTime { get; set; }

    [Display(Name = "Country Reference")]
    public Guid CountryReference { get; set; }
}
```

- [ ] **Step 3: Create `Region` (references `Country`)**

`src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Region.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class Region
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Region Name")]
    public string? RegionName { get; set; }

    [Display(Name = "Region Code")]
    public char RegionCode { get; set; }

    [Display(Name = "Is International")]
    public bool IsInternational { get; set; }

    [Display(Name = "Office Count")]
    public int OfficeCount { get; set; }

    [Display(Name = "Average Deal Size")]
    [Precision(18, 2)]
    public decimal AverageDealSize { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Established Date")]
    public DateTime EstablishedDate { get; set; }

    [Display(Name = "Standard Shipping Window")]
    public TimeSpan StandardShippingWindow { get; set; }

    [Display(Name = "Region Reference")]
    public Guid RegionReference { get; set; }

    public int CountryId { get; set; }
    public virtual Country? Country { get; set; }
}
```

- [ ] **Step 4: Create `Customer` (references `Region` — the one nullable hop)**

`src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Customer.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class Customer
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Display(Name = "Customer Grade")]
    public char CustomerGrade { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Employee Count")]
    public int EmployeeCount { get; set; }

    [Display(Name = "Credit Limit")]
    [Precision(18, 2)]
    public decimal CreditLimit { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Founded Date")]
    public DateTime FoundedDate { get; set; }

    [Display(Name = "Support Response Time")]
    public TimeSpan SupportResponseTime { get; set; }

    [Display(Name = "Account Reference")]
    public Guid AccountReference { get; set; }

    // Deliberately nullable: the one optional hop in the chain, so a customer can have no region.
    public int? RegionId { get; set; }
    public virtual Region? Region { get; set; }
}
```

- [ ] **Step 5: Create `Order` (references `Customer`)**

`src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Order.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class Order
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Order Number")]
    public string? OrderNumber { get; set; }

    [Display(Name = "Order Class")]
    public char OrderClass { get; set; }

    [Display(Name = "Is Rush")]
    public bool IsRush { get; set; }

    [Display(Name = "Line Count")]
    public int LineCount { get; set; }

    [Display(Name = "Total Amount")]
    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Order Date")]
    public DateTime OrderDate { get; set; }

    [Display(Name = "Processing Window")]
    public TimeSpan ProcessingWindow { get; set; }

    [Display(Name = "Confirmation Code")]
    public Guid ConfirmationCode { get; set; }

    public int CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }
}
```

- [ ] **Step 6: Create `OrderItem` (the primary/`TDataItem` entity, references `Order`)**

`src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/OrderItem.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;

public class OrderItem
{
    [Display(Name = "Id")]
    public int Id { get; set; }

    [Display(Name = "Product Name")]
    public string? ProductName { get; set; }

    [Display(Name = "Product Grade")]
    public char ProductGrade { get; set; }

    [Display(Name = "Is Backordered")]
    public bool IsBackordered { get; set; }

    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [Display(Name = "Unit Price")]
    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Rating")]
    public RatingEnum Rating { get; set; }

    [Display(Name = "Shipped Date")]
    public DateTime? ShippedDate { get; set; }

    [Display(Name = "Estimated Duration")]
    public TimeSpan EstimatedDuration { get; set; }

    [Display(Name = "Tracking Reference")]
    public Guid TrackingReference { get; set; }

    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }
}
```

- [ ] **Step 7: Wire the five new entities into `SampleDbContext`**

Modify `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Context/SampleDbContext.cs` — replace its full contents with:

```csharp
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Context;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }

    public DbSet<DbSampleItemOne> DbSampleItemOne { get; set; }
    public DbSet<DbSampleItemTwo> DbSampleItemTwo { get; set; }

    public DbSet<DbSampleItemThree> DbSampleItemThree { get; set; }
    public DbSet<DbSampleItemFour> DbSampleItemFour { get; set; }

    public DbSet<AuthArea> AuthArea { get; set; }
    public DbSet<AuthScope> AuthScope { get; set; }

    public DbSet<Country> Country { get; set; }
    public DbSet<Region> Region { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Order> Order { get; set; }
    public DbSet<OrderItem> OrderItem { get; set; }
}
```

- [ ] **Step 8: Build to verify it compiles**

Run: `dotnet build src/Maviray.Blazor.Components.Pro.Grid.Sample.Database`
Expected: `Build succeeded.` `0 Warning(s)` `0 Error(s)`

- [ ] **Step 9: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Enums/RatingEnum.cs src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Country.cs src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Region.cs src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Customer.cs src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/Order.cs src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Models/OrderItem.cs src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Context/SampleDbContext.cs
git commit -m "feat: add OrderItem->Order->Customer->Region->Country entity chain"
```

---

### Task 3: EF Core migration for the new entity chain

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Migrations/<timestamp>_AddOrderDomain.cs` (and its `.Designer.cs` companion, plus an updated `SampleDbContextModelSnapshot.cs`) — all generated by the EF Core CLI, not hand-written.

**Interfaces:**
- Consumes: the five entity classes and `SampleDbContext` DbSets from Task 2.
- Produces: a migration that creates the `Country`, `Region`, `Customer`, `Order`, `OrderItem` tables with the correct FK relationships (including the nullable `Customer.RegionId`). Task 7 applies this migration to the real database.

- [ ] **Step 1: Confirm the `dotnet-ef` tool is available**

Run: `dotnet ef --version`
Expected: prints a version (e.g. `Entity Framework Core .NET Command-line Tools 10.0.9`). If this fails with "command not found", run `dotnet tool install --global dotnet-ef` first.

- [ ] **Step 2: Generate the migration**

Run (from the repository root):

```bash
dotnet ef migrations add AddOrderDomain --project src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Maviray.Blazor.Components.Pro.Grid.Sample.Database.csproj --startup-project src/Maviray.Blazor.Components.Pro.Grid.Sample/Maviray.Blazor.Components.Pro.Grid.Sample.csproj
```

Expected output ends with something like:
```
Done. To undo this action, use 'ef migrations remove'
```
and three new/changed files appear under `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Migrations/`: a new `<timestamp>_AddOrderDomain.cs`, a new `<timestamp>_AddOrderDomain.Designer.cs`, and an updated `SampleDbContextModelSnapshot.cs`.

This command only builds the model in-memory to diff the schema — it does **not** require a live database connection, so it works even if the `SampleDb` SQL Server instance in `appsettings.json` isn't reachable from this environment.

- [ ] **Step 3: Inspect the generated migration's `Up` method**

Open the new `<timestamp>_AddOrderDomain.cs` file and confirm it creates five tables (`Country`, `Region`, `Customer`, `Order`, `OrderItem`), that `Customer`'s `RegionId` foreign key column is nullable, and that every other new foreign key column is non-nullable. If any of this looks wrong, it means an entity in Task 2 has an incorrect nullability annotation — fix the entity, delete the migration (`dotnet ef migrations remove` with the same `--project`/`--startup-project` flags), and regenerate.

- [ ] **Step 4: Build to verify everything still compiles**

Run: `dotnet build Maviray.Blazor.Components.Pro.slnx`
Expected: `Build succeeded.` `0 Warning(s)` `0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Migrations
git commit -m "feat: add EF Core migration for the order domain entity chain"
```

---

### Task 4: `OrderDomainSeeder`

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Seeding/OrderDomainSeeder.cs`
- Modify: `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Seeding/SampleDbSeeder.cs`

**Interfaces:**
- Consumes: the entity classes from Task 2 and `SampleDbContext` (a live, already-constructed instance — same pattern `SeedSampleItemOneAsync(SampleDbContext context)` already uses).
- Produces: `public static class OrderDomainSeeder` with `public static Task SeedAsync(SampleDbContext context)`. Called from `SampleDbSeeder.SeedAsync` alongside the two existing seed calls. Task 7 runs this (via the app's startup seeding call) to populate the database before the browser verification.

- [ ] **Step 1: Create the seeder**

`src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Seeding/OrderDomainSeeder.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Context;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Seeding;

// Seeds the OrderItem -> Order -> Customer -> Region -> Country chain used by the
// "Custom Property Mapping" sample page. Every 4th customer is deliberately left without a
// region, so the grid's coalesced columns (RegionRatingOrDefault / IsInternationalOrDefault)
// have real broken-chain data to resolve against.
public static class OrderDomainSeeder
{
    private static readonly string[] CountryNames = ["Freedonia", "Sylvania", "Genovia", "Wakanda", "Kravozhia"];
    private static readonly string[] RegionNames = ["Northlands", "Southshore", "Eastfield", "Westgate", "Central Plains"];

    private static readonly string[] CompanyNames =
    [
        "Acme Corp", "Globex", "Initech", "Umbrella Inc", "Stark Industries",
        "Wayne Enterprises", "Hooli", "Wonka Industries", "Cyberdyne", "Soylent Co"
    ];

    private static readonly string[] ProductNames =
    [
        "Widget", "Gadget", "Gizmo", "Doohickey", "Contraption",
        "Thingamajig", "Sprocket", "Bracket", "Fastener", "Component"
    ];

    public static async Task SeedAsync(SampleDbContext context)
    {
        if (await context.Country.AnyAsync())
        {
            return;
        }

        var random = new Random(99);
        var ratingValues = Enum.GetValues<RatingEnum>();

        var countries = CountryNames.Select((name, i) => new Country
        {
            CountryName = name,
            CountryGrade = (char)('A' + i % 5),
            IsMemberState = i % 2 == 0,
            PopulationMillions = random.Next(5, 300),
            GdpTrillion = Math.Round((decimal)(random.NextDouble() * 20), 2),
            Rating = ratingValues[i % ratingValues.Length],
            IndependenceDate = DateTime.Today.AddYears(-random.Next(20, 250)),
            AverageTransitTime = TimeSpan.FromHours(random.Next(1, 24)),
            CountryReference = Guid.NewGuid()
        }).ToList();

        context.Country.AddRange(countries);
        await context.SaveChangesAsync();

        var regions = RegionNames.Select((name, i) => new Region
        {
            RegionName = name,
            RegionCode = (char)('A' + i % 5),
            IsInternational = i % 2 == 1,
            OfficeCount = random.Next(1, 50),
            AverageDealSize = Math.Round((decimal)(random.NextDouble() * 100000), 2),
            Rating = ratingValues[i % ratingValues.Length],
            EstablishedDate = DateTime.Today.AddYears(-random.Next(1, 100)),
            StandardShippingWindow = TimeSpan.FromHours(random.Next(1, 24)),
            RegionReference = Guid.NewGuid(),
            CountryId = countries[i % countries.Count].Id
        }).ToList();

        context.Region.AddRange(regions);
        await context.SaveChangesAsync();

        const int customerCount = 12;
        var customers = Enumerable.Range(1, customerCount).Select(i =>
        {
            // Every 4th customer deliberately has no region.
            var region = i % 4 == 0 ? null : regions[i % regions.Count];

            return new Customer
            {
                CompanyName = $"{CompanyNames[i % CompanyNames.Length]} {i}",
                CustomerGrade = (char)('A' + i % 5),
                IsActive = i % 3 != 0,
                EmployeeCount = random.Next(5, 5000),
                CreditLimit = Math.Round((decimal)(random.NextDouble() * 500000), 2),
                Rating = ratingValues[i % ratingValues.Length],
                FoundedDate = DateTime.Today.AddYears(-random.Next(1, 80)),
                SupportResponseTime = TimeSpan.FromHours(random.Next(1, 24)),
                AccountReference = Guid.NewGuid(),
                RegionId = region?.Id
            };
        }).ToList();

        context.Customer.AddRange(customers);
        await context.SaveChangesAsync();

        const int orderCount = 30;
        var orders = Enumerable.Range(1, orderCount).Select(i => new Order
        {
            OrderNumber = $"ORD-{1000 + i}",
            OrderClass = (char)('A' + i % 5),
            IsRush = i % 5 == 0,
            LineCount = random.Next(1, 20),
            TotalAmount = Math.Round((decimal)(random.NextDouble() * 20000), 2),
            Rating = ratingValues[i % ratingValues.Length],
            OrderDate = DateTime.Today.AddDays(-random.Next(0, 365)),
            ProcessingWindow = TimeSpan.FromHours(random.Next(1, 24)),
            ConfirmationCode = Guid.NewGuid(),
            CustomerId = customers[i % customers.Count].Id
        }).ToList();

        context.Order.AddRange(orders);
        await context.SaveChangesAsync();

        const int orderItemCount = 80;
        var orderItems = Enumerable.Range(1, orderItemCount).Select(i => new OrderItem
        {
            ProductName = $"{ProductNames[i % ProductNames.Length]} {i}",
            ProductGrade = (char)('A' + i % 5),
            IsBackordered = i % 6 == 0,
            Quantity = random.Next(1, 100),
            UnitPrice = Math.Round((decimal)(random.NextDouble() * 500), 2),
            Rating = ratingValues[i % ratingValues.Length],
            ShippedDate = i % 5 == 0 ? null : DateTime.Today.AddDays(-random.Next(0, 60)),
            EstimatedDuration = TimeSpan.FromHours(random.Next(1, 24)),
            TrackingReference = Guid.NewGuid(),
            OrderId = orders[i % orders.Count].Id
        }).ToList();

        context.OrderItem.AddRange(orderItems);
        await context.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Wire it into `SampleDbSeeder`**

Modify `src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Seeding/SampleDbSeeder.cs` — change the `SeedAsync` method (near the top of the file) from:

```csharp
    public static async Task SeedAsync(IDbContextFactory<SampleDbContext> factory)
    {
        await using var context = await factory.CreateDbContextAsync();

        await SeedSampleItemOneAsync(context);
        await SeedScopedItemsAsync(context);
    }
```

to:

```csharp
    public static async Task SeedAsync(IDbContextFactory<SampleDbContext> factory)
    {
        await using var context = await factory.CreateDbContextAsync();

        await SeedSampleItemOneAsync(context);
        await SeedScopedItemsAsync(context);
        await OrderDomainSeeder.SeedAsync(context);
    }
```

(No new `using` needed — `OrderDomainSeeder` is in the same `Maviray.Blazor.Components.Pro.Grid.Sample.Database.Seeding` namespace as `SampleDbSeeder`.)

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build src/Maviray.Blazor.Components.Pro.Grid.Sample.Database`
Expected: `Build succeeded.` `0 Warning(s)` `0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Seeding/OrderDomainSeeder.cs src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Seeding/SampleDbSeeder.cs
git commit -m "feat: seed the order domain entity chain"
```

---

### Task 5: `OrderItemGridModel`

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample/Models/OrderItemGridModel.cs`

**Interfaces:**
- Consumes: `OrderItem`, `RatingEnum` (Task 2).
- Produces: `public class OrderItemGridModel : IGridCollectionModel<OrderItem, int>` with the properties named exactly as below. Task 6 (the page) references `OrderItemGridModel` and every one of these property names in its `GridPropertyPathMap`.

- [ ] **Step 1: Create the grid model**

`src/Maviray.Blazor.Components.Pro.Grid.Sample/Models/OrderItemGridModel.cs`:

```csharp
using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Models;

// Demonstrates the four ways a grid column ends up sortable/filterable (or not):
//   (a) direct 1:1 with OrderItem itself - resolved via identical-name fallback
//   (b) straight passthrough of one related property - resolved via GridPropertyPathMap
//   (c) a coalesced default over a related property - resolved via GridPropertyPathMap with nullDefault
//   (d) genuinely computed from multiple properties - unresolvable, Sortable/Filterable stay false
public class OrderItemGridModel : IGridCollectionModel<OrderItem, int>
{
    // (a) Direct 1:1 with OrderItem - Mapster auto-maps these; ConstructFrom doesn't touch them.
    [Display(Name = "Id")]
    [GridDataProperty(1)]
    public int Id { get; set; }

    [Display(Name = "Product Name")]
    [GridDataProperty(2)]
    public string? ProductName { get; set; }

    [Display(Name = "Is Backordered")]
    [GridDataProperty(3)]
    public bool IsBackordered { get; set; }

    [Display(Name = "Quantity")]
    [GridDataProperty(4)]
    public int Quantity { get; set; }

    [Display(Name = "Unit Price")]
    [GridDataProperty(5)]
    public decimal UnitPrice { get; set; }

    // (b) Straight 1:1 passthrough of one related property, no default substitution.
    [Display(Name = "Order Number")]
    [GridDataProperty(6)]
    public string? OrderNumber { get; set; }

    [Display(Name = "Customer Company")]
    [GridDataProperty(7)]
    public string? CustomerCompanyName { get; set; }

    [Display(Name = "Country")]
    [GridDataProperty(8)]
    public string? CountryName { get; set; }

    // (c) Constructed via a coalesce/default over a related property.
    [Display(Name = "Region Rating (or Default)")]
    [GridDataProperty(9)]
    public RatingEnum RegionRatingOrDefault { get; set; }

    [Display(Name = "Is International (or Default)")]
    [GridDataProperty(10)]
    public bool IsInternationalOrDefault { get; set; }

    // (d) Genuinely computed - no single backing path, so these stay unresolvable.
    [Display(Name = "Total Line Value")]
    [GridDataProperty(11)]
    public decimal TotalLineValue { get; set; }

    [Display(Name = "Full Description")]
    [GridDataProperty(12)]
    public string? FullDescription { get; set; }

    public int Key => Id;

    public void ConstructFrom(OrderItem dataItem)
    {
        OrderNumber = dataItem.Order?.OrderNumber;
        CustomerCompanyName = dataItem.Order?.Customer?.CompanyName;
        CountryName = dataItem.Order?.Customer?.Region?.Country?.CountryName;

        RegionRatingOrDefault = dataItem.Order?.Customer?.Region?.Rating ?? RatingEnum.Low;
        IsInternationalOrDefault = dataItem.Order?.Customer?.Region?.IsInternational ?? false;

        TotalLineValue = dataItem.Quantity * dataItem.UnitPrice;
        FullDescription = $"{dataItem.ProductName} ({dataItem.Order?.OrderNumber})";
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/Maviray.Blazor.Components.Pro.Grid.Sample`
Expected: `Build succeeded.` `0 Warning(s)` `0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Grid.Sample/Models/OrderItemGridModel.cs
git commit -m "feat: add OrderItemGridModel demonstrating all four sort/filter resolution categories"
```

---

### Task 6: `PropertyMapping.razor` page

**Files:**
- Create: `src/Maviray.Blazor.Components.Pro.Grid.Sample/Components/Pages/PropertyMapping.razor`

**Interfaces:**
- Consumes: `OrderItem` (Task 2), `OrderItemGridModel` (Task 5), `GridPropertyPathMap<TDataItem,TModel>`/`GridIncludeConfigurator<TDataItem>`/`IGenericGridDataFactory<TDataItem,TModel,TKey>.BuildDataCollection(..., includeConfigurator:, propertyPathMap:)` (all pre-existing library APIs, with the coalescing fix from Task 1).
- Produces: a page at route `/property-mapping`. Task 7 runs the app and navigates to this route to verify sort/filter behavior end-to-end.

- [ ] **Step 1: Create the page**

`src/Maviray.Blazor.Components.Pro.Grid.Sample/Components/Pages/PropertyMapping.razor`:

```razor
@page "/property-mapping"
@using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid
@using Maviray.Blazor.Components.Pro.Core.Models.Grid
@using Maviray.Blazor.Components.Pro.Core.Models.Grid.Filter
@using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums
@using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models
@using Maviray.Blazor.Components.Pro.Grid.Sample.Models
@using Maviray.Blazor.Components.Pro.Grid.UI.Components
@using Microsoft.EntityFrameworkCore

<PageTitle>Custom Property Mapping</PageTitle>

<div class="p-4 flex flex-col gap-4">
    <div class="text-sm text-zinc-600 max-w-3xl">
        This grid shows <code>OrderItem</code>, whose <code>OrderItemGridModel</code> mixes four
        kinds of columns: direct one-to-one properties (<strong>Id</strong>, <strong>Product Name</strong>,
        <strong>Is Backordered</strong>, <strong>Quantity</strong>, <strong>Unit Price</strong>), properties
        mapped through related entities via <code>GridPropertyPathMap</code> (<strong>Order Number</strong>,
        <strong>Customer Company</strong>, <strong>Country</strong>), properties resolved the same way but
        with a coalesced default for a customer with no region (<strong>Region Rating (or Default)</strong>,
        <strong>Is International (or Default)</strong>), and two genuinely computed columns with no single
        backing path (<strong>Total Line Value</strong>, <strong>Full Description</strong>) &mdash; those two
        have no sort-click affordance and no filter input, since they can't be resolved.
    </div>

    <MaviGrid @ref="_grid"
              TDataItem="OrderItem"
              TModel="OrderItemGridModel"
              TKey="int"
              FetchData="FetchGridData"
              Parameters="_gridParameters" />
</div>

@code
{
    private readonly MaviGridParameters _gridParameters = new()
    {
        EnableFilters = true
    };

    private MaviGrid<OrderItem, OrderItemGridModel, int>? _grid;

    [Inject]
    private IGenericGridDataFactory<OrderItem, OrderItemGridModel, int>? DataFactory { get; set; }

    private static readonly GridPropertyPathMap<OrderItem, OrderItemGridModel> PropertyPathMap = new GridPropertyPathMap<OrderItem, OrderItemGridModel>()
        .Map(m => m.OrderNumber, "Order.OrderNumber")
        .Map(m => m.CustomerCompanyName, "Order.Customer.CompanyName")
        .Map(m => m.CountryName, "Order.Customer.Region.Country.CountryName")
        .Map(m => m.RegionRatingOrDefault, "Order.Customer.Region.Rating", nullDefault: RatingEnum.Low)
        .Map(m => m.IsInternationalOrDefault, "Order.Customer.Region.IsInternational", nullDefault: false);

    private static IQueryable<OrderItem> BuildQuery(IQueryable<OrderItem> query) =>
        query
            .Include(i => i.Order)
            .ThenInclude(o => o.Customer)
            .ThenInclude(c => c.Region)
            .ThenInclude(r => r.Country);

    private async Task<MaviGridCollection<int>?> FetchGridData(MaviGridDataQueryParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.OrderByColumnKey))
        {
            parameters.OrderByColumnKey = "Id";
        }

        if (DataFactory == null)
        {
            return null;
        }

        return await DataFactory.BuildDataCollection(parameters, includeConfigurator: BuildQuery, propertyPathMap: PropertyPathMap);
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/Maviray.Blazor.Components.Pro.Grid.Sample`
Expected: `Build succeeded.` `0 Warning(s)` `0 Error(s)`

If the build reports a `CS8602` nullable-warning on the `Include`/`ThenInclude` chain (dereference of a possibly null reference), add a null-forgiving operator at each hop and rebuild:

```csharp
    private static IQueryable<OrderItem> BuildQuery(IQueryable<OrderItem> query) =>
        query
            .Include(i => i.Order)
            .ThenInclude(o => o!.Customer)
            .ThenInclude(c => c!.Region)
            .ThenInclude(r => r!.Country);
```

- [ ] **Step 3: Build the whole solution to confirm no regressions**

Run: `dotnet build Maviray.Blazor.Components.Pro.slnx`
Expected: `Build succeeded.` `0 Warning(s)` `0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add src/Maviray.Blazor.Components.Pro.Grid.Sample/Components/Pages/PropertyMapping.razor
git commit -m "feat: add /property-mapping sample page"
```

---

### Task 7: End-to-end verification

**Files:** none (verification only).

**Interfaces:** none — this task exercises everything built in Tasks 1-6 against a real database and a running app.

- [ ] **Step 1: Apply the migration to the real database**

Run:

```bash
dotnet ef database update --project src/Maviray.Blazor.Components.Pro.Grid.Sample.Database/Maviray.Blazor.Components.Pro.Grid.Sample.Database.csproj --startup-project src/Maviray.Blazor.Components.Pro.Grid.Sample/Maviray.Blazor.Components.Pro.Grid.Sample.csproj
```

Expected output ends with `Done.` and no errors. This connects to the SQL Server instance configured in `src/Maviray.Blazor.Components.Pro.Grid.Sample/appsettings.json` (`ConnectionStrings:SampleDb`). If this instance is unreachable from this environment, report **DONE_WITH_CONCERNS** with the exact connection error rather than skipping the remaining steps silently.

- [ ] **Step 2: Run the app**

Run (in the background, since it stays running until stopped):

```bash
dotnet run --project src/Maviray.Blazor.Components.Pro.Grid.Sample --launch-profile http
```

Expected output includes `Now listening on: http://localhost:5296` and, shortly after, log lines showing the EF Core seeders running (the existing `DbSampleItemOne` seed plus the new `OrderDomainSeeder` — no errors). Leave it running for the next steps.

- [ ] **Step 3: Load the new page and verify column behavior**

Navigate to `http://localhost:5296/property-mapping` (or fetch its HTML with `curl http://localhost:5296/property-mapping` if a browser isn't available) and confirm:

- The grid renders with all 12 columns from `OrderItemGridModel`, in `[GridDataProperty]` sequence order.
- Clicking the header of a category (a)/(b)/(c) column (e.g. `Order Number` or `Region Rating (or Default)`) re-sorts the grid (the header gains an active sort chevron, and `Id` order changes).
- Typing into the filter input under a category (a)/(b)/(c) column narrows the rows.
- The `Total Line Value` and `Full Description` column headers (category d) have no sort-click affordance (muted/non-interactive styling per the Task 9 UI change from the earlier feature work) and there is no filter input rendered underneath them.
- At least one row shows `Region Rating (or Default)` = `Low` and `Is International (or Default)` = unchecked/false even though that row's underlying customer has no region — confirming the coalesced default is applied both in the displayed cell and in sort/filter behavior (sort by `Region Rating (or Default)` ascending should group that row with the other `Low`-rated rows, not separately). This is the exact scenario Task 1's fix made possible — without it, loading this page would throw `InvalidOperationException`.

- [ ] **Step 4: Stop the app**

Stop the background `dotnet run` process (e.g. `Ctrl+C` in its terminal, or the equivalent for however it was launched).

- [ ] **Step 5: Report results**

No commit for this task (nothing changes). Report which of the Step 3 checks passed, and paste any unexpected console/browser errors encountered.

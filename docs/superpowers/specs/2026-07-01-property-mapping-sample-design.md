# Design: End-to-End Sample for Custom Property Mapping (Sort/Filter Resolution)

Date: 2026-07-01
Status: Approved — ready for implementation plan

## Purpose

The grid sort/filter property-mapping feature (`GridPropertyPathMap<TDataItem,TModel>`,
`MaviGridColumn.Sortable`/`.Filterable`, `GenericGridDataFactory`'s resolution chain) was built
and merged with only synthetic unit-test fixtures (`TestOrder`/`TestOrderGridModel`). This design
adds a real, running showcase in `Maviray.Blazor.Components.Pro.Grid.Sample` that exercises the
full feature end-to-end against an actual EF Core-backed grid, mirroring the depth and shape of
the real-world `PurchaseOrderItem` reference case that originally motivated the feature.

## Domain model

**Chain:** `OrderItem` (primary `TDataItem`) → `Order` → `Customer` → `Region` (nullable hop) →
`Country`. Single linear chain, 5 entities, 4 hops deep at the far end — deep enough to require
genuine multi-segment dot-paths in `GridPropertyPathMap`, without the added complexity of
replicating the real-world example's branching.

Every entity carries the same "full primitive set" so every supported `MaviGridPropertyType`
(`String`, `Char`, `Bool`, `Int`, `Decimal`, `Enum`, `DateTime`, `TimeSpan`, `Guid`) is exercised
at every hop depth:

| Entity | string | char | bool | int | decimal | enum (`RatingEnum`) | DateTime | TimeSpan | Guid |
|---|---|---|---|---|---|---|---|---|---|
| `OrderItem` | `ProductName` | `ProductGrade` | `IsBackordered` | `Quantity` | `UnitPrice` | `Rating` | `ShippedDate` (nullable) | `EstimatedDuration` | `TrackingReference` |
| `Order` | `OrderNumber` | `OrderClass` | `IsRush` | `LineCount` | `TotalAmount` | `Rating` | `OrderDate` | `ProcessingWindow` | `ConfirmationCode` |
| `Customer` | `CompanyName` | `CustomerGrade` | `IsActive` | `EmployeeCount` | `CreditLimit` | `Rating` | `FoundedDate` | `SupportResponseTime` | `AccountReference` |
| `Region` | `RegionName` | `RegionCode` | `IsInternational` | `OfficeCount` | `AverageDealSize` | `Rating` | `EstablishedDate` | `StandardShippingWindow` | `RegionReference` |
| `Country` | `CountryName` | `CountryGrade` | `IsMemberState` | `PopulationMillions` | `GdpTrillion` | `Rating` | `IndependenceDate` | `AverageTransitTime` | `CountryReference` |

Every entity also has an `Id` (int, PK). Relationships:

- `OrderItem.OrderId → Order` — required (`int` FK, non-nullable `Order` nav).
- `Order.CustomerId → Customer` — required.
- `Customer.RegionId → Region` — **nullable** (`int?` FK, nullable `Region?` nav). This is the
  one deliberately-optional hop in the chain — some seeded customers have no region, which is
  what lets the demo exercise `nullDefault` coalescing against a real broken-chain case (the same
  role `PurchaseOrder?.InvoiceReceived` played in the real-world reference).
- `Region.CountryId → Country` — required.

New shared enum `RatingEnum { Low, Medium, High, Critical }` (new file, alongside the existing
`SampleEnum`), reused by every entity's `Rating` property. Distinct from `SampleEnum` to avoid
conflating this demo's domain with the existing `DbSampleItemOne` demo.

**Files (all under `Maviray.Blazor.Components.Pro.Grid.Sample.Database`):**
- `Models/OrderItem.cs`, `Models/Order.cs`, `Models/Customer.cs`, `Models/Region.cs`,
  `Models/Country.cs` — new entity classes.
- `Enums/RatingEnum.cs` — new enum.
- `Context/SampleDbContext.cs` — add 5 new `DbSet<>` properties.
- A new EF Core migration adding the 5 new tables (generated via `dotnet ef migrations add`,
  not hand-written).

## Grid model — `OrderItemGridModel`

A flat model (`Maviray.Blazor.Components.Pro.Grid.Sample/Models/OrderItemGridModel.cs`,
`IGridCollectionModel<OrderItem, int>`) — every property is `[GridDataProperty]`, none are
`[GridNavProperty]` sub-models. Four categories, matching exactly onto the resolution mechanism
`GenericGridDataFactory` implements:

### (a) Direct 1:1 with `OrderItem` — resolved via identical-name fallback (no map entry)

`Id`, `ProductName`, `IsBackordered`, `Quantity`, `UnitPrice`. Mapster auto-maps these；
`ConstructFrom` does not touch them (same pattern as today's `DbSampleItemOneModel`, whose
`ConstructFrom` is empty).

### (b) Straight 1:1 passthrough of one related property, no default substitution — resolved via `GridPropertyPathMap`, no `nullDefault`

| Model property | `ConstructFrom` assignment | Map entry |
|---|---|---|
| `OrderNumber` | `dataItem.Order?.OrderNumber` | `.Map(m => m.OrderNumber, "Order.OrderNumber")` |
| `CustomerCompanyName` | `dataItem.Order?.Customer?.CompanyName` | `.Map(m => m.CustomerCompanyName, "Order.Customer.CompanyName")` |
| `CountryName` | `dataItem.Order?.Customer?.Region?.Country?.CountryName` | `.Map(m => m.CountryName, "Order.Customer.Region.Country.CountryName")` |

`CountryName` is null on rows whose customer has no region — that's expected, unmapped-default
behavior for category (b) (no coalescing applied), same as the real-world reference's
`PurchaseOrderNumber`.

### (c) Constructed via a coalesce/default over a related property — resolved via `GridPropertyPathMap` *with* `nullDefault`

| Model property | `ConstructFrom` assignment | Map entry |
|---|---|---|
| `RegionRatingOrDefault` | `dataItem.Order?.Customer?.Region?.Rating ?? RatingEnum.Low` | `.Map(m => m.RegionRatingOrDefault, "Order.Customer.Region.Rating", nullDefault: RatingEnum.Low)` |
| `IsInternationalOrDefault` | `dataItem.Order?.Customer?.Region?.IsInternational ?? false` | `.Map(m => m.IsInternationalOrDefault, "Order.Customer.Region.IsInternational", nullDefault: false)` |

Both go through the one nullable hop (`Customer.Region`), proving `Expression.Coalesce` works
through `GridPropertyExpressionBuilder` for both an enum and a bool.

### (d) Genuinely computed in `ConstructFrom`, no single backing path — stays unresolvable

| Model property | `ConstructFrom` assignment | Resolution |
|---|---|---|
| `TotalLineValue` | `dataItem.Quantity * dataItem.UnitPrice` | No map entry, no identically-named `OrderItem` member → `Sortable = false`, `Filterable = false` |
| `FullDescription` | `$"{dataItem.ProductName} ({dataItem.Order?.OrderNumber})"` | Same — combines two different entities, no single dot-path exists |

## Page and wiring

**File:** `Maviray.Blazor.Components.Pro.Grid.Sample/Components/Pages/PropertyMapping.razor`
**Route:** `/property-mapping`
**Title:** "Custom Property Mapping"

Same shape as `Home.razor`/`Scoped.razor`: injects
`IGenericGridDataFactory<OrderItem, OrderItemGridModel, int>`, renders
`<MaviGrid TDataItem="OrderItem" TModel="OrderItemGridModel" TKey="int" FetchData="FetchGridData" Parameters="_gridParameters" />`
with `EnableFilters = true`. A short explanatory paragraph (styled like `Scoped.razor`'s) lists
which columns are sortable/filterable and why, so the demo is self-documenting in the browser
without needing to read the model source.

`FetchGridData` passes two things through `BuildDataCollection` that no existing sample page uses:

1. A `GridIncludeConfigurator<OrderItem>` static method (`BuildQuery`) doing
   `query.Include(i => i.Order).ThenInclude(o => o.Customer).ThenInclude(c => c.Region).ThenInclude(r => r.Country)`
   — required because these are scalar-flattened properties, not `[GridNavProperty]` sub-models,
   so the existing `NavigationIncludePaths` auto-include mechanism doesn't reach them. Mirrors the
   real-world `PurchaseOrderItem.BuildQuery` reference exactly.
2. A static `GridPropertyPathMap<OrderItem, OrderItemGridModel>` field with the five `.Map(...)`
   calls from categories (b) and (c) above, passed as `propertyPathMap:` to `BuildDataCollection`.

No new `IGridDataSource<>`/DI registration is needed: `SampleDbContextDataSource<TDataItem>` is
already generic over any entity in `SampleDbContext`, and `Program.cs`'s
`AddTransient(typeof(IGridDataSource<>), typeof(SampleDbContextDataSource<>))` /
`RegisterGridDataFactory()` already cover any `TDataItem`/`TModel`/`TKey` combination.

## Seeding

A new `OrderDomainSeeder` static class (parallel to the existing seeder, in
`Maviray.Blazor.Components.Pro.Grid.Sample.Database/Seeding/`), invoked from
`SampleDbSeeder.SeedAsync` alongside the existing seed calls. Generates:

- 3-5 `Country` rows, 3-5 `Region` rows (each linked to a `Country`), ~10 `Customer` rows (each
  linked to a `Region` — **except every 4th customer, which gets `RegionId = null`** so the
  coalescing paths have real broken-chain data), ~20-30 `Order` rows (each linked to a
  `Customer`), and 50-100 `OrderItem` rows (each linked to an `Order`).
- A fixed-seed `Random` (matching the existing seeder's determinism), varied values across all
  nine primitive-type properties per entity so every column has realistic variety to sort/filter
  on — including some `null` `ShippedDate` values on `OrderItem` (independent of the
  Customer→Region nullability, just ordinary nullable-column variety).
- Guarded with an `AnyAsync()` check per table, matching the existing seeder's idempotency
  pattern.

## Testing / verification plan

This is a demo/sample, not a library change — no new xUnit tests. Verification is:

1. `dotnet build Maviray.Blazor.Components.Pro.slnx` — confirms the new entities, migration,
   model, and page all compile together.
2. `dotnet ef database update` (or equivalent) applies the new migration cleanly against the
   existing `SampleDb` SQL Server instance.
3. Run the Sample app (via the `/run` skill or manually), navigate to `/property-mapping`, and
   confirm:
   - Category (a)/(b)/(c) columns are clickable to sort and have a working filter input.
   - A category (c) column (e.g. `IsInternationalOrDefault`) sorts/filters a broken-chain row
     (customer with no region) as if it held the default value, not as a separate "null" bucket.
   - Category (d) columns (`TotalLineValue`, `FullDescription`) render their computed values but
     have no sort-click affordance (non-clickable header, muted styling) and no filter input
     rendered underneath them.

## Out of scope

- No changes to `Home.razor`, `Scoped.razor`, or their existing models/entities.
- No new `IGridDataSource<>` implementation or DI changes.
- No automated UI tests (no bUnit infrastructure exists in this repo, consistent with the
  decision already made for the library feature itself).
- No branching entity relationships (single linear chain only, per the approved chain-shape
  decision).

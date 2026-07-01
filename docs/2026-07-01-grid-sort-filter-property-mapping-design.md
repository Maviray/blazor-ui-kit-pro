# Design: Sorting/Filtering for Custom-Constructed Grid Model Properties

Date: 2026-07-01
Status: Draft — pending review

## Problem

`GenericGridDataFactory<TDataItem, TModel, TKey>.BuildDataCollection` was recently extended
with an optional `GridIncludeConfigurator<TDataItem>? includeConfigurator` parameter, to let
consumers eager-load navigation graphs (via EF Core `Include`/`ThenInclude`) before
`ConstructCollection` maps each row:

```csharp
var gridModelItem = dataItem.Adapt<TModel>(MappingConfig); // Mapster: 1:1 property mapping
gridModelItem.ConstructFrom(dataItem);                     // custom/manual construction hook
```

(`GenericGridDataFactory.cs:151-152`)

This allows `TModel` properties to be built from arbitrary logic instead of a direct
1:1 property match with `TDataItem` — e.g. pulling values across a navigation chain, or
computing a value from multiple source fields.

Sorting and filtering, however, both operate directly against `TDataItem`, using the grid
column `Key` (which is a `TModel` property name) as a raw property-path string:

- **Sort:** `QueryableExtensions.OrderByDynamic` (`QueryableExtensions.cs:9-63`) builds
  `Expression.PropertyOrField(property, prop)` against `TEntity` (`TDataItem`), called from
  `EntityFrameworkGridDataSource.Get()` with `queryParameters.OrderByColumnKey`.
- **Filter:** `QueryBuilder.BuildFilterExpression` / `GetPropertyExpression`
  (`QueryBuilder.cs:10-73`) does the same against `TDataItem`, called from
  `GenericGridDataFactory.ConstructFilter` (`GenericGridDataFactory.cs:161-178`) with the
  filter's `Key`.

Both assume `TModel.PropertyName == TDataItem.PropertyName`. When a property is built inside
`ConstructFrom` with no identically named `TDataItem` counterpart, resolving the expression
throws (`Expression.PropertyOrField` / `InvalidOperationException: Property '...' not found.`).

There is currently no way to know, per property, whether it was auto-mapped by Mapster or
custom-built in `ConstructFrom` — `ConstructFrom` is a `void` method on
`IGridCollectionModel<TDataItem, TKey>` (`IGridCollectionModel.cs:8`) with no introspectable
metadata. `MaviGridColumn` (`MaviGridColumn.cs`) also has no `Sortable`/`Filterable` concept —
today every `[GridDataProperty]`/`[GridNavProperty]` column is unconditionally sort/filter
wired in `MaviGrid.razor`.

## Real-world validation

`PurchaseOrderItemGridModel.ConstructFrom` (consumer: `C:\Repos\ePCSWeb\src`,
`ePCS.Application/Models/Grid/Procurement/PurchaseOrderItemGridModel.cs`) is a representative
worst case: it eager-loads a 5-level navigation graph off `PurchaseOrderItem` via a
`GridIncludeConfigurator` (`Include(i => i.PurchaseOrder).ThenInclude(po => po.PurchaseRequisition)...`)
and builds 13 grid-exposed properties from it. Classifying each:

| Class | Properties | Count | Notes |
|---|---|---|---|
| (a) Direct 1:1 | `Id`, `Name`, `PriceEach`, `XworksDate`, `Quantity`, `PurchaseOrderItemStatus` | 6 | Already works today (Mapster) |
| (b) Navigation dot-path | `Project`, `PurchaseOrderNumber`, `Customer`, `Currency`, `OrderDate`, `InvoiceReceived` | 6 | e.g. `Customer` = `PurchaseOrder.PurchaseRequisition.Mr.Job.CustomerLegalEntity.Name` |
| (c) Irreducibly computed | `Total` (`Quantity * PriceEach`) | 1 | No backing column or path exists |

**Finding:** 12 of 13 properties (92%) are not actually arbitrary computation — they are
still single EF navigation dot-paths off `TDataItem`, of the same dot-separated shape
(`"PurchaseOrder.Currency.Code"`) that `OrderByDynamic` and `QueryBuilder` already parse via
`.Split('.')`. Only genuinely aggregated/derived values (`Total`) have no SQL-translatable
equivalent.

This means a lightweight **name → dot-path mapping** resolves the overwhelming majority of
real cases without introducing expression-tree APIs, and only true aggregates need to fall
back to "disabled."

## Goals

- Calling `BuildDataCollection` with a `TModel` that has custom-constructed, non-1:1
  properties must not throw.
- Properties reachable via a `TDataItem` navigation path should remain sortable and
  filterable, translated to that path.
- Properties with no reachable path (pure computed/aggregated values) should be clearly
  excluded from sort/filter, not silently broken or silently wrong.
- No breaking changes to `IGridCollectionModel<TDataItem, TKey>` or existing consumers that
  rely on today's 1:1 auto-mapping (`ConstructFrom` stays a `void`, opaque hook).

## Non-goals

- Making purely computed/aggregated properties (e.g. `Total`) sortable/filterable at the
  database level. (Out of scope: DB computed columns/views, in-memory post-materialization
  sort — the latter breaks correctness under paging and is explicitly rejected.)
- Introducing full LINQ `Expression<Func<TDataItem, object>>`-based mapping. Rejected in
  favor of the simpler string dot-path form, since it already matches the internal
  representation `OrderByDynamic`/`QueryBuilder` consume and covers the validated real-world
  case without added type-system friction.

## Design

### 1. Property path map

A new optional per-factory-call (or per-registration) map from `TModel` property name to a
`TDataItem` dot-path string:

```csharp
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
}

internal sealed record GridPropertyPath(string DataItemPath, object? NullDefault);
```

- `Map` takes a strongly-typed `TModel` property selector (compile-time safety, refactor-safe
  on the `TModel` side) and a dot-path string for the `TDataItem` side (matching the format
  `OrderByDynamic`/`QueryBuilder` already accept).
- The optional `nullDefault` parameter covers properties where `ConstructFrom` coalesces a
  nullable navigation value to a default (e.g. `dataItem.PurchaseOrder?.InvoiceReceived ??
  false`). When supplied, the resolved expression wraps the final property access in
  `Expression.Coalesce(property, Expression.Constant(nullDefault, underlyingType))` instead of
  a bare property access, so `NULL` at the database sorts/filters identically to the
  in-memory default — see §2 and the note below on `InvoiceReceived`.
- Consumers register the map alongside `includeConfigurator`, since the two are typically
  authored together (the map's dot-paths are exactly the navigations the include tree
  eager-loads):

```csharp
private static readonly GridPropertyPathMap<PurchaseOrderItem, PurchaseOrderItemGridModel> PropertyPaths = new GridPropertyPathMap<PurchaseOrderItem, PurchaseOrderItemGridModel>()
    .Map(m => m.Project, "PurchaseOrder.PurchaseRequisition.Mr.Job.JobNumber")
    .Map(m => m.PurchaseOrderNumber, "PurchaseOrder.Name")
    .Map(m => m.Customer, "PurchaseOrder.PurchaseRequisition.Mr.Job.CustomerLegalEntity.Name")
    .Map(m => m.Currency, "PurchaseOrder.Currency.Code")
    .Map(m => m.OrderDate, "PurchaseOrder.IssueDate")
    .Map(m => m.InvoiceReceived, "PurchaseOrder.InvoiceReceived", nullDefault: false);

return await DataFactory.BuildDataCollection(parameters, null, null, BuildQuery, PropertyPaths);
```

### 2. Resolution order in `GenericGridDataFactory`

When resolving a sort or filter key (`GenericGridDataFactory.cs:84` for sort,
`ConstructFilter`/`GenericGridDataFactory.cs:172` for filter), resolve the `TDataItem`
path in this order:

1. **Explicit map** — if `propertyPathMap` was supplied and has an entry for this `TModel`
   property name, use its `GridPropertyPath` (dot-path plus optional `NullDefault`).
2. **Identical-name fallback** — otherwise, if `TDataItem` has a property/path of the same
   name (today's existing behavior), use it as-is (no coalescing — this path has no
   `NullDefault` to apply).
3. **Unresolvable** — neither exists: treat the column as not sortable/not filterable
   (see §3) rather than letting `Expression.PropertyOrField` throw.

This preserves 100% of existing behavior for consumers who never pass a map (step 2 is
exactly what happens today), while letting mapped consumers resolve navigation-path
properties, and unmapped/unmappable properties degrade gracefully instead of throwing.

**Implementation note:** today `OrderByDynamic` (`QueryableExtensions.cs:9-63`) and
`QueryBuilder.GetPropertyExpression` (`QueryBuilder.cs:70-73`) each independently take a raw
property-name/path string and walk it via `.Split('.')` + `Expression.PropertyOrField`. To
support `NullDefault`, both need to build off a shared resolved-expression step instead of a
bare string: walk the dot-path as today, then — only when the resolved `GridPropertyPath` in
step 1 carries a `NullDefault` — wrap the final expression in `Expression.Coalesce(property,
Expression.Constant(nullDefault, Nullable.GetUnderlyingType(property.Type) ??
property.Type))` before handing it to `OrderBy`/`OrderByDescending` (sort) or the filter
criteria's condition-building `switch` (filter). Extracting this into one shared helper
(rather than duplicating the coalesce logic in both `QueryableExtensions` and
`QueryBuilder`) avoids the two call sites drifting apart. Paths resolved via step 2
(identical-name fallback) skip the coalesce step entirely — behavior there is unchanged from
today.

### 3. `Sortable` / `Filterable` on `MaviGridColumn`

Add two flags:

```csharp
public bool Sortable { get; set; } = true;
public bool Filterable { get; set; } = true;
```

Computed once per column when it's built in `ConstructCollectionRow`
(`GenericGridDataFactory.cs:294-317`), based on the resolution in §2: `false` only when
none of the three resolution steps succeed for that property.

`MaviGrid.razor` changes:
- Sort click handler (`SortByColumn`, around `MaviGrid.razor:61`) — no-op (or visually
  disabled header, no click affordance) when `column.Sortable == false`.
- Filter input row (around `MaviGrid.razor:113-123`) — don't render the filter input for a
  column when `column.Filterable == false` (or render disabled with a tooltip explaining why).

`GenericGridDataFactory.BuildDataCollection`/`ConstructFilter` — if an incoming
`OrderByColumnKey` or filter `Key` resolves to "unresolvable," skip it (fall back to default
sort / drop that filter term) rather than throwing, so a stale client request (e.g. a
previously-sortable column that became unmappable) degrades safely instead of crashing the
request.

### 4. Backward compatibility

- `propertyPathMap` is an additional optional trailing parameter on `BuildDataCollection`
  (default `null`), so existing call sites compile unchanged.
- With no map supplied, behavior is byte-for-byte identical to today: same-name resolution,
  same throw-free paths for properties that already match, same behavior for properties that
  don't (except no longer throwing — see below).
- The one behavior change for existing consumers: a currently-throwing scenario (mismatched
  name, no map) becomes a non-throwing "column not sortable/filterable" instead. This is a
  strict improvement (crash → graceful degradation) and doesn't change behavior for any
  configuration that works today.

## Known limitation

`InvoiceReceived = dataItem.PurchaseOrder?.InvoiceReceived ?? false` — resolved via
`.Map(m => m.InvoiceReceived, "PurchaseOrder.InvoiceReceived", nullDefault: false)`, so a
`null` in the database is coalesced to `false` at the SQL level (`COALESCE(...)`), matching
the in-memory `?? false` in `ConstructFrom` exactly for both sort and filter. This relies on
the consumer explicitly declaring the default via `nullDefault` — a mapped path with no
`nullDefault` sorts/filters `NULL` however the database naturally orders it (typically
separate from `false`), so authors should supply `nullDefault` whenever `ConstructFrom` uses
`??` on the same source value, to keep the two in sync.

`Total` (`Quantity * PriceEach`) and `InventoryType` (`CatalogueItem?.GetInventoryType()`)
remain unresolvable — no dot-path, coalesced or otherwise, reaches a derived/aggregated
value like these. They have no entry in the map and are not expected to gain one — they end
up `Sortable = false` / `Filterable = false`.

## Testing

- Unit tests for `GenericGridDataFactory` resolution order: mapped property, identical-name
  fallback, unresolvable property (assert `Sortable`/`Filterable` is `false`, no throw).
- Unit test for `nullDefault` coalescing: a mapped nullable navigation property with rows
  having `NULL` at the database sorts/filters as the default value, matching what
  `ConstructFrom`'s `??` would have produced in-memory.
- Unit test confirming an incoming sort/filter request for an unresolvable key is dropped
  rather than throwing.
- Integration-style test modeled on the `PurchaseOrderItemGridModel` case: a `TModel` with a
  mix of (a)/(b)/(c) properties, verifying sort/filter works for (a) and (b) and is disabled
  for (c).
- Regression test: existing sample grids (`Home.razor`, `Scoped.razor`, no map supplied)
  continue to sort/filter unchanged.

## Open questions for reviewer

1. Naming: `GridPropertyPathMap<TDataItem, TModel>` vs. folding this into
   `GridIncludeConfigurator` as a combined "shaping" object — keeping them separate types
   seems cleaner as it doesn't overload the include-only shaping delegate.
2. Should `Sortable`/`Filterable` be independently settable per property (a property might be
   filterable via `Contains` client-side-only but never sortable), or is a single resolvability
   check enough for both? This design ties both to the same resolution, on the reasoning that
   if a SQL-translatable path exists it works for both; open to revisiting if a real case needs
   them decoupled.
3. Whether unresolvable incoming sort keys should fall back to the default sort column (`Id`)
   or simply leave the query unsorted for that request.

# Design: MaviGrid data pipeline, access policies and fetch observers

Date: 2026-11-10
Status: Implemented
First consumer: ePCS (`Maviray.Epcs`)

## Goal

Page through very large tables fast, loading only the rows of the page on screen while keeping the sort and the
filters, and let each consuming app control who may read which rows, with no knowledge of its permission model in the
library.

## Principle

The library provides mechanisms; apps supply rules. MaviGrid knows nothing about roles, claims, tenants, scopes or audit
formats. It defines:

- **when** a check runs: before every fetch, per data item type;
- **what** a check may answer: not applicable, allowed (optionally with a row filter), or denied;
- **how** the grid shows each answer: rows, narrowed rows, or an access-denied state;
- **what** the app is told afterwards: the keys of the rows loaded, or that access was denied.

## Components

| Layer | Type | Responsibility |
|---|---|---|
| UI | `MaviGrid<TKey>` | Holds the page, sort, filter, selection and column-visibility state; asks `FetchData(MaviGridQuery, CancellationToken)` for one page; renders the page and its states |
| Core | `IGridDataFactory<TDataItem, TModel, TKey>.BuildPageAsync(query, GridFetchOptions?, ct)` | Access policies → one combined filter → count → page → rows → observers |
| Core | `IGridDataSource<TDataItem>` | `CountAsync` and `GetPageAsync` over a `GridDataSourceQuery<TDataItem>`; stateless |
| EF | `EntityFrameworkGridDataSource<TDataItem>` | Short-lived context per call, no tracking, primary-key tie-breaker, `ToListAsync` |
| Core | `IGridAccessPolicy`, `GridAccessPolicy<TEntity>` | Access decisions |
| Core | `IGridFetchObserver` | Notified after every fetch |
| Core | `IGridGlobalFilterProvider` | Synchronous row constraints for every grid |

## Fetch pipeline

1. **Cancellation.** The token is checked before any work and passed to every policy, observer and data source call.
2. **Access.** Policies run in registration order.
   - The first `Denied` ends evaluation.
   - The row filters of every `Allowed` are AND-combined.
   - When no policy allows and `GridDataFactoryOptions.RequireExplicitAllow` is true, the fetch is denied.
   - A policy exception or a `null` decision fails the fetch.
3. **Denied fetch.**
   - No data source or global filter call.
   - The page has the model's columns (none sortable or filterable), no rows and zero totals, with
     `AccessDenied = true` and the policy's message.
   - Observers get `OnAccessDeniedAsync`.
4. **Filter.** Column filters, `GridFetchOptions.Filter`, global filters and policy row filters are AND-combined into one
   predicate over one parameter, used for both the count and the page.
   - Column filter and sort keys are accepted only for `TModel` columns that resolve to a data item path (a member with
     the same name, or a `GridPropertyPathMap` entry). Other keys are ignored, so no request can sort or filter on a
     property the grid doesn't show.
   - Filter values are carried in a `StrongBox<object>`, so relational providers send them as parameters and reuse one
     query plan.
5. **Count, then page.**
   - The page size is clamped to `[1, MaxPageSize]` and the page number to the available pages.
   - The page query is skipped when the count is zero.
6. **Rows.**
   - Each entity is mapped to `TModel` with a Mapster map function compiled once per type (references preserved), then
     `ConstructFrom` runs.
   - Cells are formatted with property getters compiled once per model type.
   - Columns are immutable and cached per path map, so a page allocates only rows and cells.
   - Cells are value types aligned with the columns (`row.Cells[column.Index]`).
7. **Observers.** `OnRowsLoadedAsync` gets the row keys in display order, the page, the size and the total, including
   for empty pages. Exceptions propagate.

## EF data source

- One context per call from `IGridDbContextFactory` (an adapter over `IDbContextFactory<TDbContext>`), disposed when the
  call ends.
- `AsNoTracking`, includes, filter, then order. The order is the requested sort followed by each primary-key property
  in the sort direction, unless the sort is the whole key. With no requested sort, rows are ordered by the primary key.
  Keyless types and shadow-property keys get no tie-breaker.
- `Skip`/`Take`, then `ToListAsync(ct)`.
- Logging is source-generated; the filter text is built only when Debug logging is enabled.

## UI

- **Rendering.**
  - `MaviGrid` implements `IHandleEvent`, so events render only when a handler changes visible state.
  - Rows are `MaviGridRowView` components that re-render only when their row, the layout version, their selection or
    their menu changes.
  - CSS strings are computed once per layout change.
- **Fetches.**
  - Each fetch has its own `CancellationTokenSource`; a newer fetch or disposal cancels it.
  - A cancelled or superseded fetch's result or error is discarded.
  - After a fetch the grid takes the page number the server returned.
- **States.**
  - Prerender placeholder (`FetchWhilePrerendering = false` during static rendering).
  - Fetch error (`FetchErrorTitle` or `FetchErrorContent`, a Retry button, `OnFetchError`).
  - Access denied (`AccessDeniedContent`, the policy's message or `AccessDeniedTitle`; header and main menu kept;
    filters, select column and pager hidden).
  - No data.
  - The error and denied states use `role="alert"`, the others `role="status"`.
- **State kept by the grid.**
  - The initial sort and page size come from `MaviGridParameters`; after that the user's choices win.
  - Selection and hidden columns persist across pages.
  - The column dialog is mounted only while open.
- **App callbacks.** Exceptions thrown in the app's callbacks are not caught by the grid.

## Registration

```csharp
services.AddMaviGrid(o => o.RequireExplicitAllow = true)   // scoped factory, validated options
    .AddEntityFrameworkDataSource<AppDbContext>()
    .AddGlobalFilter<SoftDeleteFilter>()
    .AddAccessPolicy<AppAccessPolicy>()
    .AddFetchObserver<AuditObserver>();
```

Registrations by type are scoped and added once even when repeated (`TryAddEnumerable`); registrations by instance
are singletons.

## Tests

- `Maviray.Blazor.Components.Pro.Core.Tests`: paging, columns and formatting, the filter allow-list, parameterized
  values, access policies, observers, `ForInterface` and DI.
- `Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests` (SQLite): stable paging, tie-breakers for composite and
  keyless keys, SQL parameters, the count-only empty result, and DI end to end.
- `Maviray.Blazor.Components.Pro.Grid.UI.Tests` (bUnit): rendering, row render skipping, sort and paging, filters,
  selection, the column dialog, error, cancellation and disposal, access denied, prerender, and callbacks.

## Out of scope

- Column-level access, and authorization of row actions or cell clicks (apps check those where they handle the click).
- Persisting prerendered rows to the interactive grid (`[PersistentState]`).
- Reusing the count when only the page changes, projecting only the needed columns, and keyset paging.

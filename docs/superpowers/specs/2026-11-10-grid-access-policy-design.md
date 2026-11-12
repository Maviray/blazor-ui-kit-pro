# Design: MaviGrid data pipeline, access policies and fetch observers

Date: 2026-11-10
Status: Implemented. Review fixes, fast paging, column access, projection and prerender reuse added 2026-11-12.

## Goal

Page through very large tables fast, loading only the rows of the page on screen while keeping the sort and the
filters, and let each consuming app control who may read which rows, with no knowledge of its permission model in the
library.

## Principle

The library provides mechanisms; apps supply rules. MaviGrid knows nothing about roles, claims, tenants, scopes or audit
formats. It defines:

- **when** a check runs: before every fetch, per data item type;
- **what** a check may answer: not applicable, allowed (optionally with a row filter), or denied, and which columns
  to hide;
- **how** the grid shows each answer: rows, narrowed rows, fewer columns, or an access-denied state;
- **what** the app is told afterwards: the keys of the rows loaded, or that access was denied.

## Components

| Layer | Type | Responsibility |
|---|---|---|
| UI | `MaviGrid<TKey>` | Holds the page, sort, filter, selection and column-visibility state; asks `FetchData(MaviGridQuery, CancellationToken)` for one page; renders the page and its states |
| Core | `IGridDataFactory<TDataItem, TModel, TKey>.BuildPageAsync(query, GridFetchOptions?, ct)` | Access policies → one combined filter → count → page → rows → observers |
| Core | `IGridDataSource<TDataItem>` | `CountAsync` and `GetPageAsync` over a `GridDataSourceQuery<TDataItem>`; stateless |
| Core | `IGridKeysetDataSource<TDataItem>` | Optional: `GetKeysetPageAsync` seeks from a cursor and returns the cursors of the page's first and last rows |
| Core | `GridKeyset<T>` | Keyset order, cursors and seek over any `IQueryable<T>`, for data sources and apps' own query code |
| Core | `GridPageBuilder<TDataItem, TModel, TKey>` | Builds a page (columns, cells, cursors, hidden columns, denied state) from rows an app loaded itself; the factory uses it too |
| Core | `GridProjection<TDataItem>`, `SelectPaths` | Loads only the members a grid needs into new data items |
| EF | `EntityFrameworkGridDataSource<TDataItem>` | Short-lived context per call, no tracking, primary-key tie-breaker, keyset seek, `ToListAsync` |
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
   - `GridAccessPolicy<TEntity>` applies to `TEntity` and every type derived from it (for an interface, every type
     that implements it), with its row filter rebound to the grid's type: member access on the parameter targets the
     derived type directly (interfaces go through `GridFilterExpression.ForInterface`), so EF Core translates it. A
     grid over a subtype can't bypass its base type's policy. A policy for a derived type doesn't cover its base.
   - **Column access.** `Allow(…).WithHiddenColumns("Salary")` (or `NotApplicable().WithHiddenColumns(…)`, which hides
     columns without allowing rows) names column keys the user may not see. The hidden columns of every decision are
     combined. From there on the factory works with the column set less those columns (re-indexed, cached per hidden
     set so the grid keeps its layout): they are not in the page, no cell is formatted for them, their sort and filter
     keys are ignored (so values can't be guessed by filtering), and a projection doesn't load their paths. Unknown
     keys are ignored; a denial can't hide columns.
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
   - A value the column's type can't hold (`99999999999` for an `int`, `2026-13-45` for a date) makes that filter
     match no rows, logged at Debug, instead of failing the fetch on every keystroke.
5. **Count, then page.**
   - The page size is clamped to `[1, MaxPageSize]` and the page number to the available pages.
   - The page query is skipped when the count is zero.
   - **Count reuse.** When `MaviGridQuery.KnownTotalRows` is set and above zero, and `ReuseTotalRows` is on (the
     default), the count is skipped. If the loaded page doesn't hold the number of rows that total predicts, rows were
     added or removed since: the factory counts again and, if the page is now past the end, loads the new last page.
     A stale total on a full middle page can't be detected; it lasts until the filters change or the grid refreshes,
     and the same holds for a policy row filter that changes between fetches (apps call `RefreshAsync` when access
     changes).
   - **Keyset.** With an `IGridKeysetDataSource`, the page loads through `GetKeysetPageAsync`, and
     `MaviGridQuery.Cursor` is passed on when the requested page wasn't clamped. The page carries the last row's cursor
     as `NextPageCursor` (except on the last page) and the first row's as `PreviousPageCursor` (except on page 1).
     The reload after a stale count skips instead.
   - **Projection.** With `GridFetchOptions.Projection`, the query carries `SelectPaths`: the data item paths of the
     visible columns plus the members the projection lists (what `ConstructFrom` and `GetContextActions` read that
     isn't a column path), and no include paths. The data source loads new data items with only those members set
     (`x => new T { A = x.A, Nav = x.Nav == null ? null : new Nav { B = x.Nav.B } }`, built once per path set), after
     filtering, ordering and paging. Every other member has its default value, so an unlisted member the model reads
     comes out as its default: projection is opt-in per grid. The data item and related types need a parameterless
     constructor and settable members.
6. **Rows.**
   - Each entity is mapped to `TModel` with a Mapster map function compiled once per type (references preserved), then
     `ConstructFrom` runs.
   - Cells are formatted with property getters compiled once per model type, for the visible columns only.
   - Columns are immutable and cached per path map (and hidden set), so a page allocates only rows and cells.
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
- With `SelectPaths`, no navigation is included, and the page is selected with `SelectPaths` plus the primary key
  (which orders every page and positions its cursors).
- **Keyset.** `GetKeysetPageAsync` builds a `GridKeyset` with the same order (sort, then the primary key). With a
  cursor it loads `WHERE k1 >= v1 AND (k1 > v1 OR (k1 = v1 AND k2 > v2) …)` and `Take`, with no `OFFSET`; seeking
  backwards reverses the order and then the loaded rows. Seek values are parameters, and every comparison runs in the
  database, so it follows the database's own order:
  - strings compare with `string.Compare` and Guids with `CompareTo`, both translated to `>`/`<` (so SQL Server's
    `uniqueidentifier` byte order and a column's collation apply);
  - enums compare by their underlying number;
  - `bool` has no ordering operator: with the cursor's value known, `b > false` becomes `b`, `b < true` becomes `NOT b`,
    and the impossible comparisons become constants.

  It seeks only when every key can be: the sort is a column of the entity itself (not through a navigation, where an
  outer join can make it null), required or given a null default, and no key has a value converter, except an enum
  stored as its underlying number (an enum stored as text orders by name, which a numeric comparison would contradict).
  Otherwise the page is skipped to and carries no cursors.
- A cursor is JSON holding the order's signature and the row's key values. A cursor from another order, or one that
  can't be read, is ignored and the page is skipped to.
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
  - Once disposed, the grid starts no fetch (a filter pause can end just before disposal).
  - A header menu rebuild asked for by `RefreshAsync` waits for the next fetch that completes, so a newer fetch that
    supersedes the refresh still rebuilds it.
- **Paging hints** in each `MaviGridQuery`, which fetch code may use or ignore:
  - `KnownTotalRows`: the current page's total, when the filters are unchanged since that page. Not sent on the first
    fetch, after a filter change, on `RefreshAsync` or the no-data reload, or after a denied or failed fetch.
  - `Cursor`: the current page's `NextPageCursor` or `PreviousPageCursor`, when the target is the adjacent page, isn't
    page 1 (skipping no rows is as fast, and shows rows added before the old first row), and the sort, filters and
    page size are unchanged.
- **Prerendered page.** With `PrerenderedPageKey` set (unique per grid on the page), the prerendering grid registers
  with `PersistentComponentState` and stores its page and the query that loaded it as JSON; a failed fetch stores
  nothing. The interactive grid takes it back and, when it answers the initial query (page, size, sort, filters),
  starts from it without fetching, then builds its header menu. Row actions come back as
  `MaviRowContextMenuItem<TKey>` with the same values, since an interface can't be read back from JSON. The paging
  hints work from the restored page. The service is resolved only when a key is set.
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
are singletons. Options are validated when the host starts (`ValidateOnStart`), not on the first fetch.

## Apps with their own query code

An app whose service layer loads pages itself (DTOs behind its own authorization) uses `MaviGrid` with its own
`FetchData` and no factory:

- `GridPageBuilder<TDto, TRow, TKey>.Build(new GridLoadedPage<TDto>(items, page, size, total) { NextPageCursor = …,
  PreviousPageCursor = …, HiddenColumns = … }, query)` builds the page; `Denied(message)` builds the access-denied
  page; `Columns(map, hiddenColumns)` tells which column keys are sortable and filterable (those with a `TDto` member).
- Its query code can skip its count when `query.KnownTotalRows` is set (checking the loaded page as the factory does),
  seek with `GridKeyset<TEntity>.LoadPageAsync` when `query.Cursor` is set, and load only what a DTO holds with
  `SelectPaths` or its own select (the load delegate may project, as long as the order keys stay).

## Tests

- `Maviray.Blazor.Components.Pro.Core.Tests`: paging, count reuse, keyset paging (every seekable type, both
  directions), projection and `SelectPaths`, the page builder, columns and formatting, column access, the filter
  allow-list, out-of-range filter values, parameterized values, access policies (including subtypes and interfaces),
  observers, `ForInterface` and DI (including startup validation).
- `Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests` (SQLite): stable paging, tie-breakers for composite and
  keyless keys, SQL parameters, the count-only empty result, keyset seeks (forward, backward, composite keys, Guid
  keys, enum and bool sorts, null defaults, a projection without the key, and the orders it won't seek: nullable,
  value-converted), projected selects (no unshown columns, null navigations, still seeking), a base-type policy on a
  table-per-hierarchy subtype, and DI end to end (a next-page fetch runs one query with no count and no offset).
- `Maviray.Blazor.Components.Pro.Grid.UI.Tests` (bUnit): rendering, row render skipping, sort and paging, the paging
  hints, filters, selection, the column dialog, error, cancellation and disposal, access denied, prerender and the
  prerendered page's reuse, the menu rebuild after a superseded refresh, and callbacks.

## Fixed in the 2026-11-12 review

1. A filter value the column's type can't hold failed the fetch; it now matches no rows.
2. `GridAccessPolicy<TEntity>` didn't cover subtypes, so a grid over a derived type loaded unfiltered by default; it
   now covers them with the row filter rebound.
3. The header menu stayed stale when a newer fetch superseded a `RefreshAsync()`.
4. A filter fetch could start after disposal: a wasted query and an audit entry for a grid no one sees.
5. Options were validated on first use rather than at startup.

## Limits

- Keyset paging falls back to offset paging for a sort through a navigation, a nullable sort without a null default,
  a value-converted key (other than an enum stored as its number), and keyless or shadow-key types.
- A projection can't load through collections, and needs parameterless constructors and settable members on every
  type on its paths. What the model reads must be listed; an unlisted member reads as its default.
- A stale total on a full middle page lasts until the filters change or the grid refreshes (see count reuse).
- A prerendered page's row actions come back as `MaviRowContextMenuItem<TKey>`, not the app's own action type.

Authorization of row actions and cell clicks stays with the app, which checks access where it handles the click. That
includes `SelectedKeys`: it keeps rows selected before a denial or a narrower row filter, so an app acting on the
selection checks access again.

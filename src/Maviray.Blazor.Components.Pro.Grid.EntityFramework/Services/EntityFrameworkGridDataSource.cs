using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Maviray.Blazor.Components.Pro.Core.Extensions;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Services;

public class EntityFrameworkGridDataSource<TDataItem, TDbContext> : IGridDataSource<TDataItem> where TDataItem : class where TDbContext : DbContext
{
    private const string DB_SET_NULL = "DB Set is empty or null.";

    private readonly IDbContextFactory<TDbContext> _contextFactory;
    private readonly ILogger<EntityFrameworkGridDataSource<TDataItem, TDbContext>> _logger;

    private TDbContext? _dbContext;
    private DbSet<TDataItem>? _dbSet;

    public EntityFrameworkGridDataSource(IDbContextFactory<TDbContext> dbContextFactory, ILogger<EntityFrameworkGridDataSource<TDataItem, TDbContext>> logger)
    {
        _contextFactory = dbContextFactory;
        _logger = logger;
    }

    private async Task InitializeContext()
    {
        _dbContext ??= await _contextFactory.CreateDbContextAsync();
        _dbSet ??= _dbContext.Set<TDataItem>();
    }

    public async Task<int> Count(Expression<Func<TDataItem, bool>>? filter = null)
    {
        await InitializeContext();

        if (_dbSet == null)
        {
            throw new InvalidOperationException(DB_SET_NULL);
        }

        if (filter == null)
        {
            _logger.LogDebug
                ("Counting items in the data source.");

            return await _dbSet.CountAsync();
        }
        else
        {
            _logger.LogDebug(
                "Counting items in the data source. Filter: {Filter}",
                filter.ToString());

            return await _dbSet.Where(filter).CountAsync();
        }
    }

    public async Task<IQueryable<TDataItem>?> Get(SortOrder sortOrder, string orderBy, int skip, int numberOfItemsToTake, Expression<Func<TDataItem, bool>>? filter = null, IReadOnlyCollection<string>? includePaths = null, GridIncludeConfigurator<TDataItem>? includeConfigurator = null, object? orderByNullDefault = null)
    {
        await InitializeContext();

        if (_dbSet == null)
        {
            throw new InvalidOperationException(DB_SET_NULL);
        }

        _logger.LogDebug(
            "Getting items from data source. SortOrder: {SortOrder}, OrderBy: {OrderBy}, Skip: {Skip}, Take: {Take}, Filter: {Filter}, Includes: {Includes}, IncludeConfigurator: {IncludeConfigurator}",
            sortOrder,
            orderBy,
            skip,
            numberOfItemsToTake,
            filter?.ToString() ?? "none",
            includePaths == null || includePaths.Count == 0 ? "none" : string.Join(", ", includePaths),
            includeConfigurator == null ? "none" : "supplied");

        IQueryable<TDataItem> query = _dbSet;

        // Eager-load the requested navigation properties so nested grid columns are populated.
        if (includePaths != null)
        {
            foreach (var includePath in includePaths)
            {
                if (!string.IsNullOrWhiteSpace(includePath))
                {
                    query = query.Include(includePath);
                }
            }
        }

        // Apply the consumer-supplied include tree (e.g. Include(...).ThenInclude(...)) so related
        // entities that cannot be expressed as dotted string paths are materialized by the same paged
        // query and are available when each TModel is built in ConstructFrom.
        if (includeConfigurator != null)
        {
            query = includeConfigurator(query);
        }

        if (filter != null)
        {
            query = query.Where(filter);
        }

        return query.OrderByDynamic(orderBy, sortOrder, orderByNullDefault).Skip(skip).Take(numberOfItemsToTake);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or
    /// resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>

    #region dispose
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources here
            if (_dbContext != null)
            {
                await _dbContext.DisposeAsync();
            }
        }

        // Free unmanaged resources here, if any

        _disposed = true;
    }

    #endregion
}
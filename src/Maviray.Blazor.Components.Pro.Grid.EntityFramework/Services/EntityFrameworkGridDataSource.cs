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

    public EntityFrameworkGridDataSource(IDbContextFactory<TDbContext> dbContextFactory, ILogger<EntityFrameworkGridDataSource<TDataItem, TDbContext>> logger)
    {
        _contextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<int> Count(Expression<Func<TDataItem, bool>>? filter = null)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var dbSet = dbContext.Set<TDataItem>();
    
        if (filter == null)
        {
            _logger.LogDebug(
                "Counting items in the data source. Items Type: {ItemsType}",
                typeof(TDataItem).Name);

            return await dbSet.CountAsync();
        }
        else
        {
            _logger.LogDebug(
                "Counting items in the data source. Items Type: {ItemsType}, Filter: {Filter}",
                typeof(TDataItem).Name,
                filter.ToString());

            return await dbSet.Where(filter).CountAsync();
        }
    }

    public async Task<IQueryable<TDataItem>?> Get(SortOrder sortOrder, string orderBy, int skip, int numberOfItemsToTake, Expression<Func<TDataItem, bool>>? filter = null, IReadOnlyCollection<string>? includePaths = null, GridIncludeConfigurator<TDataItem>? includeConfigurator = null, object? orderByNullDefault = null)
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }

        _dbContext = await _contextFactory.CreateDbContextAsync();
        var dbSet = _dbContext.Set<TDataItem>();

        _logger.LogDebug(
            "Getting items from data source. Items Type: {ItemsType}, SortOrder: {SortOrder}, OrderBy: {OrderBy}, Skip: {Skip}, Take: {Take}, Filter: {Filter}, Includes: {Includes}, IncludeConfigurator: {IncludeConfigurator}",
            typeof(TDataItem).Name,
            sortOrder,
            orderBy,
            skip,
            numberOfItemsToTake,
            filter?.ToString() ?? "none",
            includePaths == null || includePaths.Count == 0 ? "none" : string.Join(", ", includePaths),
            includeConfigurator == null ? "none" : "supplied");

        IQueryable<TDataItem> query = dbSet;

        // Eager-load the requested navigation properties so nested grid columns are populated.
        if (includePaths != null)
        {
            query = includePaths.Where(includePath => !string.IsNullOrWhiteSpace(includePath)).Aggregate(query, (current, includePath) => current.Include(includePath));
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

        return query.OrderByDynamic(orderBy, sortOrder, orderByNullDefault).Skip(skip).Take(numberOfItemsToTake).AsNoTracking();
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
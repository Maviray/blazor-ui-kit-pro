using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Services;

/// <summary>
/// Creates the <see cref="DbContext"/> grid queries run on. Each data source call creates its own context and
/// disposes it when the call ends.
/// </summary>
public interface IGridDbContextFactory
{
    Task<DbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}

internal sealed class GridDbContextFactory<TDbContext>(IDbContextFactory<TDbContext> factory) : IGridDbContextFactory
    where TDbContext : DbContext
{
    public async Task<DbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
}

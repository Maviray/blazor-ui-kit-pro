using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions;

public static class MaviGridBuilderExtensions
{
    /// <summary>
    /// Reads every grid's data through EF Core with <typeparamref name="TDbContext"/>. Register the context with
    /// <c>AddDbContextFactory</c> or <c>AddPooledDbContextFactory</c>; to load related collections with one query
    /// each instead of a join, configure <c>UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)</c> there.
    /// </summary>
    public static MaviGridBuilder AddEntityFrameworkDataSource<TDbContext>(this MaviGridBuilder builder)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddScoped<IGridDbContextFactory, GridDbContextFactory<TDbContext>>();
        return builder.AddDataSource(typeof(EntityFrameworkGridDataSource<>));
    }
}

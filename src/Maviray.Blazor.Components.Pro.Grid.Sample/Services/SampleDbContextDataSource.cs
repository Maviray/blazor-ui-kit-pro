using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Services;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Services;

public class SampleDbContextDataSource<TDataItem> : EntityFrameworkGridDataSource<TDataItem, SampleDbContext> where TDataItem : class
{
    public SampleDbContextDataSource(IDbContextFactory<SampleDbContext> dbContextFactory, ILogger logger) : base(dbContextFactory, logger)
    {

    }
}
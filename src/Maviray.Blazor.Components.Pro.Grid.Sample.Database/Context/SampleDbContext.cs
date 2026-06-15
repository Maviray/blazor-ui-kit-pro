using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Context;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }

    public DbSet<DbSampleItemOne> DbSampleItemOne { get; set; }
    public DbSet<DbSampleItemTwo> DbSampleItemTwo { get; set; }
}
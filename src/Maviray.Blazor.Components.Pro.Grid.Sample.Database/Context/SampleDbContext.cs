using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Context;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }

    public DbSet<DbSampleItemOne> DbSampleItemOne { get; set; }
    public DbSet<DbSampleItemTwo> DbSampleItemTwo { get; set; }

    public DbSet<DbSampleItemThree> DbSampleItemThree { get; set; }
    public DbSet<DbSampleItemFour> DbSampleItemFour { get; set; }

    public DbSet<AuthArea> AuthArea { get; set; }
    public DbSet<AuthScope> AuthScope { get; set; }

    public DbSet<Country> Country { get; set; }
    public DbSet<Region> Region { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Order> Order { get; set; }
    public DbSet<OrderItem> OrderItem { get; set; }
}
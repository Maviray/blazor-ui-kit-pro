using Maviray.Blazor.Components.Pro.Core.Attributes.Grid;
using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests.Fixtures;

public class TestProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
}

public class TestLine
{
    public int OrderNumber { get; set; }
    public int LineNumber { get; set; }
    public string Sku { get; set; } = "";
}

public class TestProductSummary
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestProduct> Products => Set<TestProduct>();
    public DbSet<TestLine> Lines => Set<TestLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestLine>().HasKey(x => new { x.OrderNumber, x.LineNumber });
        modelBuilder.Entity<TestProductSummary>().HasNoKey().ToSqlQuery("SELECT Name, Category FROM Products");
    }
}

public class TestProductGridModel : IGridCollectionModel<TestProduct, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2)]
    public string Name { get; set; } = "";

    [GridDataProperty(3)]
    public string Category { get; set; } = "";

    public int Key => Id;

    public void ConstructFrom(TestProduct dataItem)
    {
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

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

// Table-per-hierarchy inheritance: invoices are documents stored in the same table.
public enum TestDocumentKind
{
    Memo,
    Letter,
    Report
}

public class TestDocument
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Title { get; set; } = "";
    public string? Note { get; set; }
    public TestDocumentKind Kind { get; set; }
    public bool Archived { get; set; }

    // Stored as text through a value converter, so the database orders it by name, not by number.
    public TestDocumentKind KindAsText { get; set; }
}

// A shipment with an optional carrier, and wide columns no grid shows.
public class TestShipment
{
    public int Id { get; set; }
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public int? CarrierId { get; set; }
    public TestCarrier? Carrier { get; set; }
}

public class TestCarrier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
}

public class TestCarrierModel
{
    [GridNestedProperty("Carrier")]
    public string Name { get; set; } = "";
}

public class TestShipmentGridModel : IGridCollectionModel<TestShipment, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2)]
    public string Reference { get; set; } = "";

    [GridNavProperty(3)]
    public TestCarrierModel? Carrier { get; set; }

    public int Key => Id;

    public void ConstructFrom(TestShipment dataItem)
    {
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

// A Guid primary key.
public class TestTag
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
}

public class TestInvoice : TestDocument
{
    public decimal Amount { get; set; }
}

public class TestInvoiceGridModel : IGridCollectionModel<TestInvoice, int>
{
    [GridDataProperty(1)]
    public int Id { get; set; }

    [GridDataProperty(2)]
    public string Title { get; set; } = "";

    public int Key => Id;

    public void ConstructFrom(TestInvoice dataItem)
    {
    }

    public IEnumerable<IMaviRowContextMenuItem<int>> GetContextActions() => [];
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestProduct> Products => Set<TestProduct>();
    public DbSet<TestLine> Lines => Set<TestLine>();
    public DbSet<TestDocument> Documents => Set<TestDocument>();
    public DbSet<TestInvoice> Invoices => Set<TestInvoice>();
    public DbSet<TestTag> Tags => Set<TestTag>();
    public DbSet<TestShipment> Shipments => Set<TestShipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestLine>().HasKey(x => new { x.OrderNumber, x.LineNumber });
        modelBuilder.Entity<TestDocument>().Property(x => x.KindAsText).HasConversion<string>();
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

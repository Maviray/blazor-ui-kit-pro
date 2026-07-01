using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Context;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Seeding;

// Seeds the OrderItem -> Order -> Customer -> Region -> Country chain used by the
// "Custom Property Mapping" sample page. Every 4th customer is deliberately left without a
// region, so the grid's coalesced columns (RegionRatingOrDefault / IsInternationalOrDefault)
// have real broken-chain data to resolve against.
public static class OrderDomainSeeder
{
    private static readonly string[] CountryNames = ["Freedonia", "Sylvania", "Genovia", "Wakanda", "Kravozhia"];
    private static readonly string[] RegionNames = ["Northlands", "Southshore", "Eastfield", "Westgate", "Central Plains"];

    private static readonly string[] CompanyNames =
    [
        "Acme Corp", "Globex", "Initech", "Umbrella Inc", "Stark Industries",
        "Wayne Enterprises", "Hooli", "Wonka Industries", "Cyberdyne", "Soylent Co"
    ];

    private static readonly string[] ProductNames =
    [
        "Widget", "Gadget", "Gizmo", "Doohickey", "Contraption",
        "Thingamajig", "Sprocket", "Bracket", "Fastener", "Component"
    ];

    public static async Task SeedAsync(SampleDbContext context)
    {
        if (await context.Country.AnyAsync())
        {
            return;
        }

        var random = new Random(99);
        var ratingValues = Enum.GetValues<RatingEnum>();

        var countries = CountryNames.Select((name, i) => new Country
        {
            CountryName = name,
            CountryGrade = (char)('A' + i % 5),
            IsMemberState = i % 2 == 0,
            PopulationMillions = random.Next(5, 300),
            GdpTrillion = Math.Round((decimal)(random.NextDouble() * 20), 2),
            Rating = ratingValues[i % ratingValues.Length],
            IndependenceDate = DateTime.Today.AddYears(-random.Next(20, 250)),
            AverageTransitTime = TimeSpan.FromHours(random.Next(1, 24)),
            CountryReference = Guid.NewGuid()
        }).ToList();

        context.Country.AddRange(countries);
        await context.SaveChangesAsync();

        var regions = RegionNames.Select((name, i) => new Region
        {
            RegionName = name,
            RegionCode = (char)('A' + i % 5),
            IsInternational = i % 2 == 1,
            OfficeCount = random.Next(1, 50),
            AverageDealSize = Math.Round((decimal)(random.NextDouble() * 100000), 2),
            Rating = ratingValues[i % ratingValues.Length],
            EstablishedDate = DateTime.Today.AddYears(-random.Next(1, 100)),
            StandardShippingWindow = TimeSpan.FromHours(random.Next(1, 24)),
            RegionReference = Guid.NewGuid(),
            CountryId = countries[i % countries.Count].Id
        }).ToList();

        context.Region.AddRange(regions);
        await context.SaveChangesAsync();

        const int customerCount = 12;
        var customers = Enumerable.Range(1, customerCount).Select(i =>
        {
            // Every 4th customer deliberately has no region.
            var region = i % 4 == 0 ? null : regions[i % regions.Count];

            return new Customer
            {
                CompanyName = $"{CompanyNames[i % CompanyNames.Length]} {i}",
                CustomerGrade = (char)('A' + i % 5),
                IsActive = i % 3 != 0,
                EmployeeCount = random.Next(5, 5000),
                CreditLimit = Math.Round((decimal)(random.NextDouble() * 500000), 2),
                Rating = ratingValues[i % ratingValues.Length],
                FoundedDate = DateTime.Today.AddYears(-random.Next(1, 80)),
                SupportResponseTime = TimeSpan.FromHours(random.Next(1, 24)),
                AccountReference = Guid.NewGuid(),
                RegionId = region?.Id
            };
        }).ToList();

        context.Customer.AddRange(customers);
        await context.SaveChangesAsync();

        const int orderCount = 30;
        var orders = Enumerable.Range(1, orderCount).Select(i => new Order
        {
            OrderNumber = $"ORD-{1000 + i}",
            OrderClass = (char)('A' + i % 5),
            IsRush = i % 5 == 0,
            LineCount = random.Next(1, 20),
            TotalAmount = Math.Round((decimal)(random.NextDouble() * 20000), 2),
            Rating = ratingValues[i % ratingValues.Length],
            OrderDate = DateTime.Today.AddDays(-random.Next(0, 365)),
            ProcessingWindow = TimeSpan.FromHours(random.Next(1, 24)),
            ConfirmationCode = Guid.NewGuid(),
            CustomerId = customers[i % customers.Count].Id
        }).ToList();

        context.Order.AddRange(orders);
        await context.SaveChangesAsync();

        const int orderItemCount = 80;
        var orderItems = Enumerable.Range(1, orderItemCount).Select(i => new OrderItem
        {
            ProductName = $"{ProductNames[i % ProductNames.Length]} {i}",
            ProductGrade = (char)('A' + i % 5),
            IsBackordered = i % 6 == 0,
            Quantity = random.Next(1, 100),
            UnitPrice = Math.Round((decimal)(random.NextDouble() * 500), 2),
            Rating = ratingValues[i % ratingValues.Length],
            ShippedDate = i % 5 == 0 ? null : DateTime.Today.AddDays(-random.Next(0, 60)),
            EstimatedDuration = TimeSpan.FromHours(random.Next(1, 24)),
            TrackingReference = Guid.NewGuid(),
            OrderId = orders[i % orders.Count].Id
        }).ToList();

        context.OrderItem.AddRange(orderItems);
        await context.SaveChangesAsync();
    }
}

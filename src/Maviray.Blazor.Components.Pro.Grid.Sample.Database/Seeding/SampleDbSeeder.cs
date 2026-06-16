using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Context;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Enums;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Seeding;

public static class SampleDbSeeder
{
    private static readonly string[] Names =
    [
        "Alpha", "Bravo", "Charlie", "Delta", "Echo",
        "Foxtrot", "Golf", "Hotel", "India", "Juliet",
        "Kilo", "Lima", "Mike", "November", "Oscar",
        "Papa", "Quebec", "Romeo", "Sierra", "Tango"
    ];

    public static async Task SeedAsync(IDbContextFactory<SampleDbContext> factory)
    {
        await using var context = await factory.CreateDbContextAsync();

        await SeedSampleItemOneAsync(context);
        await SeedScopedItemsAsync(context);
    }

    private static async Task SeedSampleItemOneAsync(SampleDbContext context)
    {
        if (await context.DbSampleItemOne.AnyAsync())
            return;

        var random = new Random(42);
        var enumValues = Enum.GetValues<SampleEnum>();

        var items = Enumerable.Range(1, 1000).Select(i =>
        {
            var name = $"{Names[i % Names.Length]} {i}";
            return new DbSampleItemOne
            {
                Name = name,
                Integer = random.Next(1, 1000),
                Decimal = Math.Round((decimal)(random.NextDouble() * 9999), 2),
                Long = random.NextInt64(1_000_000, 9_999_999),
                SampleEnum = enumValues[i % enumValues.Length],
                Boolean = i % 2 == 0,
                Date = i % 5 == 0 ? null : DateTime.Today.AddDays(-random.Next(0, 365)),
                DbSampleItemTwo = new DbSampleItemTwo
                {
                    Name = $"Sub {name}",
                    Description = $"Description for item {i}"
                }
            };
        }).ToList();

        context.DbSampleItemOne.AddRange(items);
        await context.SaveChangesAsync();
    }

    // Seeds the lookup tables and the scoped sample items used to demonstrate the global grid filter.
    // Guarded independently of SampleItemOne so it still runs against an already-populated database.
    private static async Task SeedScopedItemsAsync(SampleDbContext context)
    {
        if (await context.Scope.AnyAsync())
            return;

        var scopes = new[] { "Alpha", "Bravo", "Gamma" }
            .Select(name => new Scope { Name = name })
            .ToList();

        var authAreas = new[] { "North", "South", "East" }
            .Select(name => new AuthArea { Name = name })
            .ToList();

        context.Scope.AddRange(scopes);
        context.AuthArea.AddRange(authAreas);

        // Persist first so the generated identity values are available for the foreign keys below.
        await context.SaveChangesAsync();

        var three = new List<DbSampleItemThree>();
        var four = new List<DbSampleItemFour>();

        foreach (var scope in scopes)
        {
            foreach (var authArea in authAreas)
            {
                for (var n = 1; n <= 3; n++)
                {
                    three.Add(new DbSampleItemThree
                    {
                        Name = $"Three · {scope.Name}/{authArea.Name} #{n}",
                        ScopeId = scope.Id,
                        AuthAreaId = authArea.Id
                    });

                    four.Add(new DbSampleItemFour
                    {
                        Name = $"Four · {scope.Name}/{authArea.Name} #{n}",
                        ScopeId = scope.Id,
                        AuthAreaId = authArea.Id
                    });
                }
            }
        }

        context.DbSampleItemThree.AddRange(three);
        context.DbSampleItemFour.AddRange(four);
        await context.SaveChangesAsync();
    }
}

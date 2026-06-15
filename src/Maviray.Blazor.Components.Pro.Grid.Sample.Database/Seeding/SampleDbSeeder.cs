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
}

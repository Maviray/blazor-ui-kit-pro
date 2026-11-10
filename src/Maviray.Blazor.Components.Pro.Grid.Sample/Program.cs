using Maviray.Blazor.Components.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Extensions;
using Maviray.Blazor.Components.Pro.Grid.Sample.Components;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Context;
using Maviray.Blazor.Components.Pro.Grid.Sample.Database.Seeding;
using Maviray.Blazor.Components.Pro.Grid.Sample.Services;
using Microsoft.EntityFrameworkCore;

namespace Maviray.Blazor.Components.Pro.Grid.Sample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // The sample keeps its data in memory: no database server or connection string is needed.
            // The database starts empty on every run and SampleDbSeeder fills it before the app starts.
            builder.Services.AddDbContextFactory<SampleDbContext>(options =>
                options.UseInMemoryDatabase("SampleDb"));

            builder.Services.AddMaviComponents(options => options.EnableLifecycleLogging = false);

            // Per-user context the demo filter and policy read (fixed demo values; real apps derive them from the
            // authenticated user).
            builder.Services.AddScoped<IUserScopeContext, DemoUserScopeContext>();
            builder.Services.AddScoped<DemoAccessState>();
            builder.Services.AddScoped<DemoFetchLog>();

            builder.Services.AddMaviGrid()
                // Every grid reads through EF Core with SampleDbContext.
                .AddEntityFrameworkDataSource<SampleDbContext>()
                // Applied to every grid whose entity implements IScopedItem, with no per-page wiring.
                .AddGlobalFilter<ScopedItemGridFilter>()
                // Decides access before every fetch; only the Access page's grid is its concern.
                .AddAccessPolicy<DemoAccessPolicy>()
                // Told which rows every fetch loaded, for the Access page's audit list.
                .AddFetchObserver<DemoFetchObserver>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            await SampleDbSeeder.SeedAsync(
                app.Services.GetRequiredService<IDbContextFactory<SampleDbContext>>());

            app.Run();
        }
    }
}

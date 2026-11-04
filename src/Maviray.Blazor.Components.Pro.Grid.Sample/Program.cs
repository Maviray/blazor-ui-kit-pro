using Maviray.Blazor.Components.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
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

            builder.Services.RegisterGridDataFactory();

            // Per-user scope used by the global grid filter below (demo value; real apps derive this
            // from the authenticated user).
            builder.Services.AddScoped<IUserScopeContext, DemoUserScopeContext>();

            // Register the cross-cutting scope filter once: it is applied to every grid whose entity
            // implements IScopedItem, with no per-page wiring.
            builder.Services.AddGridGlobalFilter<ScopedItemGridFilter>();

            builder.Services.AddLogging();

            builder.Services.AddTransient(
                typeof(IGridDataSource<>),
                typeof(SampleDbContextDataSource<>));

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

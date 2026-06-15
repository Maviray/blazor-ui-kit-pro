using Maviray.Blazor.Components.Pro.Core.Interfaces.Grid;
using Maviray.Blazor.Components.Pro.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection RegisterGridDataFactory(this IServiceCollection services)
    {
        services.AddTransient(
            typeof(IGenericGridDataFactory<,,>),
            typeof(GenericGridDataFactory<,,>));
        return services;
    }
}
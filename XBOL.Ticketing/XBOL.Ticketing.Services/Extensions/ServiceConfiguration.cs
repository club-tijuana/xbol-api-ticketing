using Microsoft.Extensions.DependencyInjection;

namespace XBOL.Ticketing.Services.Extensions
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<RoleService>();

            return services;
        }
    }
}
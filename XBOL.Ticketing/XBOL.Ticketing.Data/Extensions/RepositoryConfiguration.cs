using Microsoft.Extensions.DependencyInjection;
using XBOL.Ticketing.Data.Repositories;

namespace XBOL.Ticketing.Data.Extensions
{
    public static class RepositoryConfiguration
    {
        public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
        {
            services.AddScoped<RoleRepository>();

            return services;
        }
    }
}
using Hangfire;
using Odasoft.XBOL.Commons.BackgroundJobs;

namespace XBOL.Ticketing.API.Extensions;

public static class BackgroundJobsConfiguration
{
    public static IServiceCollection ConfigureBackgroundJobs(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredSection("BackgroundJobs:ConnectionString").Value!;

        services.AddHangfire(config => config.UseDefaultStorage(connectionString, prepareSchemaIfNecessary: false));

        return services;
    }
}

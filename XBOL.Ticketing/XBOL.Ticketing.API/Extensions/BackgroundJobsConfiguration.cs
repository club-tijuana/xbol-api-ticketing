using Hangfire;
using Hangfire.PostgreSql;
using XBOL.Ticketing.Core.Commons.Options;

namespace XBOL.Ticketing.API.Extensions;

public static class BackgroundJobsConfiguration
{
    public static IServiceCollection ConfigureBackgroundJobs(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = new BackgroundJobsOptions();
        configuration.GetRequiredSection("BackgroundJobs").Bind(options);

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                opts => opts.UseNpgsqlConnection(options.ConnectionString),
                new PostgreSqlStorageOptions
                {
                    PrepareSchemaIfNecessary = false,
                    DistributedLockTimeout = TimeSpan.FromMinutes(1)
                }));

        return services;
    }
}

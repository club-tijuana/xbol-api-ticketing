using Hangfire;
using Hangfire.PostgreSql;

namespace Odasoft.XBOL.Commons.BackgroundJobs;

public static class BackgroundJobsServiceCollectionExtensions
{
    public static IGlobalConfiguration UseDefaultStorage(
        this IGlobalConfiguration config,
        string connectionString,
        bool prepareSchemaIfNecessary)
    {
        return config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                opts => opts.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    PrepareSchemaIfNecessary = prepareSchemaIfNecessary,
                    DistributedLockTimeout = TimeSpan.FromMinutes(1),
                });
    }
}

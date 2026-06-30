using Hangfire;
using Hangfire.PostgreSql;
using Odasoft.XBOL.Commons.Options;

namespace Odasoft.XBOL.Commons.BackgroundJobs;

public static class BackgroundJobsServiceCollectionExtensions
{
    public static IGlobalConfiguration UseDefaultStorage(
        this IGlobalConfiguration config,
        BackgroundJobsOptions options)
    {
        return config.UseDefaultStorage(
            options.ConnectionString,
            prepareSchemaIfNecessary: false);
    }

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

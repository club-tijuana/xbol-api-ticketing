using Hangfire;
using Odasoft.XBOL.Commons.BackgroundJobs;
using BackgroundJobsOptions = Odasoft.XBOL.Commons.Options.BackgroundJobsOptions;

namespace XBOL.Ticketing.API.Extensions;

public static class BackgroundJobsConfiguration
{
    public static IServiceCollection ConfigureBackgroundJobs(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = new BackgroundJobsOptions();
        configuration.GetRequiredSection("BackgroundJobs").Bind(options);

        services.AddHangfire(config => config.UseDefaultStorage(options));

        return services;
    }
}

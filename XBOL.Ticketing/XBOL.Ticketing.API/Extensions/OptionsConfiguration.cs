using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Extensions;

public static class OptionsConfiguration
{
    public static IServiceCollection ConfigureOptions(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SeatsIoOptions>()
            .BindConfiguration("SeatsIoApi")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}

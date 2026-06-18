using XBOL.Ticketing.Core.Commons.Options;
using XBOL.Ticketing.Services.EvoPayment;

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

        services.AddOptions<BackgroundJobsOptions>()
            .BindConfiguration("BackgroundJobs")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EvoSettings>()
            .BindConfiguration("EvoSettings")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DefaultExchangeRateOptions>()
            .BindConfiguration("DefaultExchangeRate")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PaymentLinkOptions>()
            .BindConfiguration("PaymentLink")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<BackgroundJobsOptions>()
            .BindConfiguration("BackgroundJobs")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailTemplateOptions>()
            .BindConfiguration("EmailTemplate")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}

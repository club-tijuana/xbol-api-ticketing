using XBOL.Ticketing.API.Options;

namespace XBOL.Ticketing.API.Extensions;

public static class CorsConfiguration
{
    public static CorsOptions GetCorsOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
    }

    public static IServiceCollection ConfigureCorsPolicy(this IServiceCollection services, CorsOptions options)
    {
        var origins = AcceptedOrigins(options);
        if (origins.Length == 0)
        {
            return services;
        }

        var policyName = PolicyName(options);
        services.AddCors(cors =>
        {
            cors.AddPolicy(policyName, policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseConfiguredCors(this IApplicationBuilder app, CorsOptions options)
    {
        return AcceptedOrigins(options).Length == 0
            ? app
            : app.UseCors(PolicyName(options));
    }

    public static string PolicyName(CorsOptions options)
    {
        return string.IsNullOrWhiteSpace(options.PolicyName)
            ? CorsOptions.DefaultPolicyName
            : options.PolicyName.Trim();
    }

    public static string[] AcceptedOrigins(CorsOptions options)
    {
        return options.AcceptedOrigins
            .Select(origin => origin.Trim())
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}

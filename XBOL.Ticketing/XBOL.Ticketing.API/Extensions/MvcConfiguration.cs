using XBOL.Ticketing.API.Filters;
using XBOL.Ticketing.API.Infrastructure;

namespace XBOL.Ticketing.API.Extensions;

public static class MvcConfiguration
{
    public static IServiceCollection ConfigureMvc(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<ApiExceptionFilter>();
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        }).AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(
                new Newtonsoft.Json.Converters.StringEnumConverter());
        });

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            };
        });

        services.AddExceptionHandler<SeatsIoExceptionHandler>();

        return services;
    }
}

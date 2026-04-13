namespace XBOL.Ticketing.API.Extensions;

public static class MvcConfiguration
{
    public static IServiceCollection ConfigureMvc(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        }).AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(
                new Newtonsoft.Json.Converters.StringEnumConverter());
        });

        return services;
    }
}

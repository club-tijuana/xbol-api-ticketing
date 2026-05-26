using XBOL.Ticketing.API.Extensions;
using XBOL.Ticketing.API.Schema;
using XBOL.Ticketing.Data.Extensions;
using XBOL.Ticketing.Services.Extensions;

if (args.Contains("--generate-schema"))
{
    var outputPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "appsettings.schema.json"));
    AppSettingsSchemaGenerator.GenerateAndWrite(outputPath);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Infrastructure
builder.Services.ConfigureOptions(builder.Configuration);
builder.Services.ConfigureDatabase(builder.Configuration);

// Security
builder.Services.ConfigureIdentity();

// Application
builder.Services.ConfigureServices();
builder.Services.ConfigureRepositories();

// Web framework
builder.Services.ConfigureMvc();
builder.Services.AddHealthChecks();
builder.Services.ConfigureSwagger();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/ticketing-api.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/ticketing-api.json", "Ticketing API");
    });

    app.MapGet(
        "/",
        context =>
        {
            context.Response.Redirect("/swagger/index.html");
            return Task.CompletedTask;
        });
}

// Only use HTTPS redirection when running directly (Visual Studio, dotnet run)
// Containers handle TLS at load balancer/reverse proxy level
if (!app.Environment.IsProduction()
    || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

var supportedCultures = new[] { "en", "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("es")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.MapControllers();

app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var appName = app.Environment.ApplicationName ?? "unknown";
        var environment = app.Environment.EnvironmentName ?? "unknown";
        var dockerImageVersion = Environment.GetEnvironmentVariable("DOCKER_IMAGE_VERSION") ?? "unknown";
        var response = new
        {
            appName,
            environment,
            status = report.Status.ToString(),
            dockerImageVersion
        };
        await context.Response.WriteAsJsonAsync(response);
    },
});

app.Run();

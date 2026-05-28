using Wolverine;
using Wolverine.Postgresql;
using XBOL.Ticketing.Services.Extensions;

namespace XBOL.Ticketing.API.Extensions;

public static class WolverineConfiguration
{
    public static IHostBuilder ConfigureWolverine(this IHostBuilder host, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default must be configured for Wolverine durable message storage.");
        }

        host.UseWolverine(opts =>
        {
            opts.PersistMessagesWithPostgresql(connectionString);
            TicketingWolverineConfiguration.ConfigureTicketingLifecycle(opts);
        });

        return host;
    }
}

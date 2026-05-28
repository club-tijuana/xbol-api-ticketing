using Wolverine;
using XBOL.Ticketing.Services.Extensions;

namespace XBOL.Ticketing.API.Extensions;

public static class WolverineConfiguration
{
    public static IHostBuilder ConfigureWolverine(this IHostBuilder host)
    {
        host.UseWolverine(TicketingWolverineConfiguration.ConfigureTicketingLifecycle);

        return host;
    }
}

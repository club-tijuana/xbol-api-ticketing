using Wolverine;
using Wolverine.EntityFrameworkCore;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.API.Extensions;

public static class WolverineConfiguration
{
    public static IHostBuilder ConfigureWolverine(this IHostBuilder host)
    {
        host.UseWolverine(opts =>
        {
            opts.UseEntityFrameworkCoreTransactions();
            opts.Discovery.IncludeAssembly(typeof(CreateSeatsIoEventCommand).Assembly);
        });

        return host;
    }
}

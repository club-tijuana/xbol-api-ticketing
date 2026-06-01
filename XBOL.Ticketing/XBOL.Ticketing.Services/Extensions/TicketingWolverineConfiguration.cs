using Wolverine;
using Wolverine.EntityFrameworkCore;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Services.Extensions;

public static class TicketingWolverineConfiguration
{
    public static void ConfigureTicketingLifecycle(WolverineOptions opts)
    {
        opts.UseEntityFrameworkCoreTransactions();
        opts.Discovery.IncludeAssembly(typeof(CreateSeatsIoEventCommand).Assembly);
    }
}

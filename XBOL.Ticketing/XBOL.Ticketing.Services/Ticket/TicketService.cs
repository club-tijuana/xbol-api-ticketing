using XBOL.Ticketing.Data.Repositories.Ticket;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Ticket
{
    public class TicketService(TicketRepository repository) : BaseService<TicketRepository, Core.Model.Ticket>(repository)
    {
    }
}

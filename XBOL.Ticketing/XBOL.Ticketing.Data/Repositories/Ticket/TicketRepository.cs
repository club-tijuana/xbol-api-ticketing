using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Ticket
{
    public class TicketRepository(XBOLDbContext dbContext)
        : BaseRepository<Core.Model.Ticket>(dbContext), ITicketRepository
    {
    }
}

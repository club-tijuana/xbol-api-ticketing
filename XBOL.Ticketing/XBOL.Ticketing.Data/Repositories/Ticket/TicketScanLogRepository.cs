using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Ticket
{
    public class TicketScanLogRepository(XBOLDbContext dbContext) : BaseRepository<TicketScanLog>(dbContext)
    {
    }
}

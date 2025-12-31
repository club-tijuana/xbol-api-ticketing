using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Ticket;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Ticket
{
    public class TicketTransferService(TicketTransferRepository repository) : BaseService<TicketTransferRepository, TicketTransfer>(repository)
    {
    }
}

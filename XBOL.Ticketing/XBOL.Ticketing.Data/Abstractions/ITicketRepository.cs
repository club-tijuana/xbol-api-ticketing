using CoreTicket = XBOL.Ticketing.Core.Model.Ticket;

namespace XBOL.Ticketing.Data.Abstractions;

public interface ITicketRepository
{
    Task<CoreTicket?> GetByIdAsync(long id);
}

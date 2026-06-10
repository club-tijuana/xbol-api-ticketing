using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Bundle
{
    public class BundlePassEventTicketRepository(XBOLDbContext dbContext)
        : BaseRepository<BundlePassEventTicket>(dbContext), IBundlePassEventTicketRepository
    {
    }
}

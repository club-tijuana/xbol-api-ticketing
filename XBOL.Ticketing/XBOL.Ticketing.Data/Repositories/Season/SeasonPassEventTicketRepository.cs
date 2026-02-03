using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Season
{
    public class SeasonPassEventTicketRepository(XBOLDbContext dbContext) : BaseRepository<SeasonPassEventTicket>(dbContext)
    {
    }
}

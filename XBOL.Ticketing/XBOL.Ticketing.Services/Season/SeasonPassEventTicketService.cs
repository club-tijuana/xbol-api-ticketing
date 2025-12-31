using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Season;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Season
{
    public class SeasonPassEventTicketService(SeasonPassEventTicketRepository repository) : BaseService<SeasonPassEventTicketRepository, SeasonPassEventTicket>(repository)
    {
    }
}

using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Order
{
    public class SeatHoldRepository(XBOLDbContext dbContext) : BaseRepository<SeatHold>(dbContext)
    {
    }
}

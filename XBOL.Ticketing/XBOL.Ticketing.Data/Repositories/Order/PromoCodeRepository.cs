using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Order
{
    public class PromoCodeRepository(XBOLDbContext dbContext) : BaseRepository<PromoCode>(dbContext)
    {
    }
}

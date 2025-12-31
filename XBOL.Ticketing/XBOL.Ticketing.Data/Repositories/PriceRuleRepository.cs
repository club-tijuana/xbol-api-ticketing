using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories
{
    public class PriceRuleRepository(XBOLDbContext dbContext) : BaseRepository<PriceRule>(dbContext)
    {
    }
}

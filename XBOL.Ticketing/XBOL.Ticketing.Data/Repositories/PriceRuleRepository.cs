using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Views;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories
{
    public class PriceRuleRepository(XBOLDbContext dbContext) : BaseRepository<PriceRule>(dbContext)
    {
        public async Task<IList<DynamicPricingRule>> GetRulesByEventScheduleId(long eventScheduleId)
        {
            return await DbSet.AsNoTracking()
                              .AsSplitQuery()
                              .Where(x => x.EventScheduleId == eventScheduleId)
                              .Select(x => new DynamicPricingRule
                              {
                                  Id = x.Id,
                                  Code = x.Code,
                                  Description = x.Description,
                                  Expression = x.Expression,
                              })
                              .ToListAsync();
        }
    }
}
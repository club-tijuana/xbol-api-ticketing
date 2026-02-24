using XBOL.Ticketing.Core.Commons.Views;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services
{
    public class PriceRuleService(PriceRuleRepository repository) : BaseService<PriceRuleRepository, PriceRule>(repository)
    {
        internal async Task<IList<DynamicPricingRule>> GetRulesByEventScheduleIdAsync(long eventScheduleId)
            => await Repository.GetRulesByEventScheduleIdAsync(eventScheduleId);
    }
}

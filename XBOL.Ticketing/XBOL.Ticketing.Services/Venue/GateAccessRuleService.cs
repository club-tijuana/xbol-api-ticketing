using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Venue
{
    public class GateAccessRuleService(GateAccessRuleRepository repository) : BaseService<GateAccessRuleRepository, GateAccessRule>(repository)
    {
    }
}

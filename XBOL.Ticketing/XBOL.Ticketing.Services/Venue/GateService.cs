using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Venue
{
    public class GateService(GateRepository repository) : BaseService<GateRepository, Gate>(repository)
    {
    }
}

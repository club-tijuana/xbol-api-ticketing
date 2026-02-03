using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Venue
{
    public class BaseRowService(BaseRowRepository repository) : BaseService<BaseRowRepository, BaseRow>(repository)
    {
    }
}

using XBOL.Ticketing.Data.Repositories.Season;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Season
{
    public class SeasonService(SeasonRepository repository) : BaseService<SeasonRepository, Core.Model.Season>(repository)
    {
    }
}

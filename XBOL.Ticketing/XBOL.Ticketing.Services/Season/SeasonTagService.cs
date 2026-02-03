using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Season;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Season
{
    public class SeasonTagService(SeasonTagRepository repository) : BaseService<SeasonTagRepository, SeasonTag>(repository)
    {
    }
}

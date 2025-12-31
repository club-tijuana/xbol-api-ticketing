using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Season;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Season
{
    public class SeasonPassService(SeasonPassRepository repository) : BaseService<SeasonPassRepository, SeasonPass>(repository)
    {
    }
}

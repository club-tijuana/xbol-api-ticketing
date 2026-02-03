using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Season
{
    public class SeasonTagRepository(XBOLDbContext dbContext) : BaseRepository<SeasonTag>(dbContext)
    {
    }
}

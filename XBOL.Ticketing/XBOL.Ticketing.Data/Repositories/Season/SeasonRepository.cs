using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Season
{
    public class SeasonRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Season>(dbContext)
    {
    }
}

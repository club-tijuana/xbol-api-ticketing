using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Media
{
    public class MediaRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Media>(dbContext)
    {
    }
}

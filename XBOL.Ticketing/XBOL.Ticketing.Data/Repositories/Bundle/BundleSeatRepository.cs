using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Bundle
{
    public class BundleSeatRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.BundleSeat>(dbContext)
    {
    }
}

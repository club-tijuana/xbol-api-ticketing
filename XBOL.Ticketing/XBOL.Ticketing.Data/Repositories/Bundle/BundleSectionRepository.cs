using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Bundle
{
    public class BundleSectionRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.BundleSection>(dbContext)
    {
    }
}

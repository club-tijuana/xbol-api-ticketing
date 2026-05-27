using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Bundle
{
    public class BundleTagRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.BundleTag>(dbContext)
    {
    }
}

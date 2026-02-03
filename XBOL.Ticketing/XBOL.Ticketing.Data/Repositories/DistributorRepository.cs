using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories
{
    public class DistributorRepository(XBOLDbContext dbContext) : BaseRepository<Distributor>(dbContext)
    {
    }
}

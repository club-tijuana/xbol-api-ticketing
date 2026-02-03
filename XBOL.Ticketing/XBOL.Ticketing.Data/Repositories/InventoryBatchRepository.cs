using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories
{
    public class InventoryBatchRepository(XBOLDbContext dbContext) : BaseRepository<InventoryBatch>(dbContext)
    {
    }
}

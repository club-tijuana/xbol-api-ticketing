using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Order
{
    public class OrderRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Order>(dbContext)
    {
    }
}

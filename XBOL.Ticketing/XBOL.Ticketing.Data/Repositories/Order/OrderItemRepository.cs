using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Order
{
    public class OrderItemRepository(XBOLDbContext dbContext) : BaseRepository<OrderItem>(dbContext)
    {
    }
}

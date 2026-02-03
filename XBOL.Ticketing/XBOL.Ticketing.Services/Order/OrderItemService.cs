using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Order;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Order
{
    public class OrderItemService(OrderItemRepository repository) : BaseService<OrderItemRepository, OrderItem>(repository)
    {
    }
}

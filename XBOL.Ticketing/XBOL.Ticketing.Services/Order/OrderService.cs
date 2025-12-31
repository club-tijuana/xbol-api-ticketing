using XBOL.Ticketing.Data.Repositories.Order;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Order
{
    public class OrderService(OrderRepository repository) : BaseService<OrderRepository, Core.Model.Order>(repository)
    {
    }
}


using XBOL.Ticketing.DynamicPricing.Models.Catalogs;

namespace XBOL.Ticketing.DynamicPricing.Models
{
    public class ItemLists
    {
        public IList<Order> Orders { get; set; } = new List<Order>();

        public IList<Seat> Seats { get; set; } = new List<Seat>();
    }
}

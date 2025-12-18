namespace XBOL.Ticketing.Core.Model
{
    public class Distributor
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Contact { get; set; } = null!;

        public IList<InventoryBatch> InventoryBatches { get; set; } = [];
    }
}
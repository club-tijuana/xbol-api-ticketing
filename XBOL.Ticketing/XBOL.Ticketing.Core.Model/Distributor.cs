namespace XBOL.Ticketing.Core.Model
{
    public class Distributor : BaseModel
    {
        public string Name { get; set; } = null!;
        public string Contact { get; set; } = null!;

        public IList<InventoryBatch> InventoryBatches { get; set; } = [];
    }
}
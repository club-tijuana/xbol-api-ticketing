namespace XBOL.Ticketing.Core.Model
{
    public class BaseRow
    {
        public long Id { get; set; }

        public long BaseSectionId { get; set; }
        public BaseSection BaseSection { get; set; } = null!;

        public string RowLabel { get; set; } = null!;

        public IList<BaseSeat> BaseSeats { get; set; } = [];
    }
}
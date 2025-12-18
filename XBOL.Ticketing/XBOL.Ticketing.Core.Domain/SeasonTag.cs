namespace XBOL.Ticketing.Core.Model
{
    public class SeasonTag
    {
        public long Id { get; set; }

        public long SeasonId { get; set; }
        public Season Season { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
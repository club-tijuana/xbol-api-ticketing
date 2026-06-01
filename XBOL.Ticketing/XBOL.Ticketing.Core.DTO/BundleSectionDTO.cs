namespace XBOL.Ticketing.Core.DTO
{
    public class BundleSectionDTO
    {
        public long Id { get; set; }
        public long BundleId { get; set; }
        public long BaseSectionId { get; set; }
        public string DisplayName { get; set; } = null!;
        public decimal? Price { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
    }
}

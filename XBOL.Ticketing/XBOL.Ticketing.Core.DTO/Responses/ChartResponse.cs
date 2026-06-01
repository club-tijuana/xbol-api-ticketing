namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class ChartResponse
    {
        public long Id { get; set; }
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public bool Archived { get; set; }
        public string PublishedVersionThumbnailUrl { get; set; } = "";
        public string DraftVersionThumbnailUrl { get; set; } = "";
        public string VenueType { get; set; } = "";
        public ChartValidationResponse Validation { get; set; } = null!;
    }

    public class ChartValidationResponse
    {
        public List<string> Errors { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
    }
}

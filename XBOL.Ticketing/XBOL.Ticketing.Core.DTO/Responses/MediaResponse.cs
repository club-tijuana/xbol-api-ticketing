using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class MediaResponse
    {
        public long Id { get; set; }
        public long ReferenceId { get; set; }
        public SaleType ReferenceType { get; set; }
        public string Title { get; set; } = "";
        public string ImageBase64 { get; set; } = "";
        public string ContentType { get; set; } = "";
        public string? Url { get; set; }
        public MediaType MediaType { get; set; }
        public int Order { get; set; }
    }
}

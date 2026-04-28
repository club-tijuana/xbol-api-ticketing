namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class SeatsIoErrorResponse
    {
        public List<SeatsIoErrorDetail> Errors { get; set; } = [];
        public string? RequestId { get; set; }
    }

    public class SeatsIoErrorDetail
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

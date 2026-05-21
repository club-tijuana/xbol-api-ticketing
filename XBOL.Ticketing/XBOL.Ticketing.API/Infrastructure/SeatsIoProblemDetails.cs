using Microsoft.AspNetCore.Mvc;

namespace XBOL.Ticketing.API.Infrastructure
{
    public sealed class SeatsIoProblemDetails : ProblemDetails
    {
        public List<SeatsIoProblemError> Errors { get; set; } = [];
        public string? RequestId { get; set; }
    }

    public sealed class SeatsIoProblemError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

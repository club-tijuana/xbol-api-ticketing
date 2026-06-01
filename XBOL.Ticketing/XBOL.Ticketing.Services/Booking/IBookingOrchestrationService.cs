using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;

namespace XBOL.Ticketing.Services.Booking
{
    public interface IBookingOrchestrationService
    {
        Task<BookingResultResponse> BookAsync(
            BookSeatsActionRequest request,
            Guid actorUserId,
            CancellationToken cancellationToken = default);
    }
}

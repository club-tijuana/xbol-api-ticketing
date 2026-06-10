using XBOL.Ticketing.Core.DTO.Requests;

namespace XBOL.Ticketing.Services.Booking
{
    public interface ISeatsIoBookingClient
    {
        Task<IReadOnlyList<string>> BookSeatsAsync(
            string eventKey,
            List<BookingSeatRequest> seats,
            string holdToken,
            CancellationToken cancellationToken = default);

        Task ReleaseBookedSeatsAsync(
            string eventKey,
            IReadOnlyCollection<string> seats,
            CancellationToken cancellationToken = default);
    }
}


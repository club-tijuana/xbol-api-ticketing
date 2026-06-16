using XBOL.Ticketing.Core.DTO.Requests;

namespace XBOL.Ticketing.Services.Booking
{
    public class SeatsIoBookingClient(SeatsIoService seatsIoService) : ISeatsIoBookingClient
    {
        public async Task<IReadOnlyList<string>> BookSeatsAsync(
            string eventKey,
            List<BookingSeatRequest> seats,
            string holdToken,
            CancellationToken cancellationToken = default)
        {
            var result = await seatsIoService.BookSeatsWithDetailsAsync(
                eventKey,
                seats,
                holdToken);

            return result.Objects.Select(x => x.Key).ToList();
        }

        public async Task<IReadOnlyList<string>> BookSeatsAsync(
            string[] eventKeys,
            List<BookingSeatRequest> seats,
            string holdToken,
            CancellationToken cancellationToken = default)
        {
            var result = await seatsIoService.BookSeatsWithDetailsAsync(
                eventKeys,
                seats,
                holdToken);

            return result.Objects.Select(x => x.Key).ToList();
        }

        public async Task ReleaseBookedSeatsAsync(
            string eventKey,
            IReadOnlyCollection<string> seats,
            CancellationToken cancellationToken = default)
        {
            await seatsIoService.ReleaseSeatsAsync(eventKey, seats.ToArray());
        }

        public async Task<bool> EventOrSeasonExistsAsync(
            string eventKey,
            CancellationToken cancellationToken = default)
        {
            return await seatsIoService.EventOrSeasonExistsAsync(eventKey);
        }

        public async Task<bool> ValidateSeatsExistAsync(
            string eventKey,
            IReadOnlyCollection<string> seats,
            CancellationToken cancellationToken = default)
        {
            return await seatsIoService.ValidateAllSeatsExistAsync(eventKey, seats.ToList());
        }
    }
}

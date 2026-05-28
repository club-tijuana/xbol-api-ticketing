using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.Services.Booking
{
    public class SeatsIoBookingClient(SeatsIoService seatsIoService) : ISeatsIoBookingClient
    {
        public async Task<IReadOnlyList<string>> BookSeatsAsync(
            string eventKey,
            IReadOnlyDictionary<string, decimal> seats,
            string holdToken,
            CancellationToken cancellationToken = default)
        {
            var result = await seatsIoService.BookSeatsWithDetailsAsync(
                eventKey,
                seats.ToDictionary(),
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
    }
}

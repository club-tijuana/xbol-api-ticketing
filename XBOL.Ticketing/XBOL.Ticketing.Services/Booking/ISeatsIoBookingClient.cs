namespace XBOL.Ticketing.Services.Booking
{
    public interface ISeatsIoBookingClient
    {
        Task<IReadOnlyList<string>> BookSeatsAsync(
            string eventKey,
            IReadOnlyDictionary<string, decimal> seats,
            string holdToken,
            CancellationToken cancellationToken = default);

        Task ReleaseBookedSeatsAsync(
            string eventKey,
            IReadOnlyCollection<string> seats,
            CancellationToken cancellationToken = default);
    }
}

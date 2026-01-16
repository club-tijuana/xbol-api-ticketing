using Microsoft.Extensions.Options;
using SeatsioDotNet;
using SeatsioDotNet.EventReports;
using SeatsioDotNet.Events;
using SeatsioDotNet.HoldTokens;
using XBOL.Ticketing.Core.DTO;

namespace XBOL.Ticketing.Services
{
    public class SeatsIoService(IOptions<SeatsIoOptions> options)
    {
        // TODO: Initialize on constructor according to event's region and key
        private readonly SeatsioClient _client = new(Region.NA(), options.Value.SecretKey);

        public async Task<ChangeObjectStatusResult> BookSeatsAsync(BookingRequest request)
        {
            List<ObjectProperties> seatsToBook = [];

            foreach (var seat in request.Seats)
            {
                seatsToBook.Add(new ObjectProperties(seat, new Dictionary<string, object> { { "salesPoint", "Admin" } }));
            }

            // TODO: Handle exceptions SeatsioException
            ChangeObjectStatusResult response = await _client.Events.BookAsync(request.EventId, seatsToBook, request.HoldToken);

            return response;
        }

        public async Task ReleaseHoldTokenAsync(string holdToken)
        {
            await _client.HoldTokens.ExpiresInMinutesAsync(holdToken, 0);
        }

        public async Task<HoldToken> SetHoldTokenExpirationAsync(string holdToken, int minutes)
        {
            return await _client.HoldTokens.ExpiresInMinutesAsync(holdToken, minutes);
        }

        public async Task<Dictionary<string, EventObjectInfo>> GetSeatsInfoAsync(string eventKey, string[] seats)
        {
            return await _client.Events.RetrieveObjectInfosAsync(eventKey, seats);
        }

        public async Task<ChangeObjectStatusResult> PutUpForResaleAsync(string eventKey, string[] seats, string? listing = null)
        {
            return await _client.Events.PutUpForResaleAsync(eventKey, seats, listing);
        }

        public async Task<ChangeObjectStatusResult> ReleaseSeatsAsync(string eventKey, string[] seats)
        {
            return await _client.Events.ReleaseAsync(eventKey, seats);
        }
    }
}

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

            string? token = null;
            // Try to retrieve hold token, if it doesn't succeed we ignore the error
            try
            {
                var holdToken = await _client.HoldTokens.RetrieveAsync(request.HoldToken);
                if (holdToken is not null && holdToken.ExpiresInSeconds > 0)
                {
                    token = holdToken.Token;
                }
            }
            catch (Exception)
            {

            }

            // TODO: Handle exceptions SeatsioException
            ChangeObjectStatusResult response = await _client.Events.BookAsync(request.EventId, seatsToBook, token);

            // Try to release hold token after booking attempt, if it doesn't succeed we ignore the error
            try
            {
                await ReleaseHoldTokenAsync(request.HoldToken);
            }
            catch (Exception)
            {
            }

            return response;
        }

        public async Task<HoldToken> CreateHoldTokenAsync()
        {
            return await _client.HoldTokens.CreateAsync();
        }

        public async Task<HoldToken> CreateHoldTokenAsync(int expirationInMinutes)
        {
            return await _client.HoldTokens.CreateAsync(expirationInMinutes);
        }

        public async Task<ChangeObjectStatusResult> HoldSeatsAsync(string eventKey, string[] seats, string holdToken)
        {
            return await _client.Events.HoldAsync(eventKey, seats, holdToken);
        }

        public async Task<HoldToken> GetHoldTokenAsync(string holdToken)
        {
            return await _client.HoldTokens.RetrieveAsync(holdToken);
        }

        public async Task<HoldToken> ReleaseHoldTokenAsync(string holdToken)
        {
            return await _client.HoldTokens.ExpiresInMinutesAsync(holdToken, 0);
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

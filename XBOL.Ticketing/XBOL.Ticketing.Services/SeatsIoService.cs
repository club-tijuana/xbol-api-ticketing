using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeatsioDotNet;
using SeatsioDotNet.Charts;
using SeatsioDotNet.EventReports;
using SeatsioDotNet.Events;
using SeatsioDotNet.HoldTokens;
using SeatsioDotNet.Reports.Events;
using SeatsioDotNet.Util;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.Services
{
    public class SeatsIoService
    {
        private readonly SeatsIoOptions _options;
        private readonly SeatsioClient _client;
        private readonly ILogger<SeatsIoService> _logger;
        private readonly EventScheduleService _eventScheduleService;

        public SeatsIoService(IOptions<SeatsIoOptions> options, ILogger<SeatsIoService> logger, EventScheduleService eventScheduleService)
        {
            _options = options.Value;
            _client = new SeatsioClient(ResolveRegion(options.Value.Region), options.Value.SecretKey);
            _logger = logger;
            _eventScheduleService = eventScheduleService;
        }

        private static Region ResolveRegion(string? region) => (region ?? "NA").ToUpperInvariant() switch
        {
            "NA" => Region.NA(),
            "EU" => Region.EU(),
            "SA" => Region.SA(),
            "OC" => Region.OC(),
            _ => throw new ArgumentException($"Unknown Seats.io region: {region}")
        };

        [Obsolete("Use BookSeatsAsync(string, Dictionary<string, decimal>, string) instead.")]
        public async Task<ChangeObjectStatusResult> BookEventSeatsAsync(EventBookingRequest request)
        {
            List<ObjectProperties> seatsToBook = [];

            foreach (KeyValuePair<string, decimal> seat in request.Seats)
            {
                seatsToBook.Add(new ObjectProperties(seat.Key, new Dictionary<string, object> { { "salesPoint", "Admin" } }));
            }

            var response = await BookSeatsAsync(request.EventKey, seatsToBook, request.HoldToken);

            return response;
        }

        [Obsolete("Use BookSeatsAsync(string, Dictionary<string, decimal>, string) instead.")]
        public async Task<ChangeObjectStatusResult> BookSeasonSeatsAsync(SeasonBookingRequest request)
        {
            List<ObjectProperties> seatsToBook = [];

            foreach (KeyValuePair<string, decimal> seat in request.Seats)
            {
                seatsToBook.Add(new ObjectProperties(seat.Key, new Dictionary<string, object> { { "salesPoint", "Admin" } }));
            }

            var response = await BookSeatsAsync(request.SeasonKey, seatsToBook, request.HoldToken);

            return response;
        }

        public async Task<ChangeObjectStatusResult> BookSeatsAsync(string eventKey, Dictionary<string, decimal> seats, string holdToken)
        {
            List<ObjectProperties> seatsToBook = [];

            foreach (KeyValuePair<string, decimal> seat in seats)
            {
                seatsToBook.Add(new ObjectProperties(seat.Key, new Dictionary<string, object> { { "salesPoint", "Admin" } }));
            }

            return await BookSeatsAsync(eventKey, seatsToBook, holdToken);
        }

        public async Task<ChangeObjectStatusResult> BookSeatsWithDetailsAsync(
            string eventKey,
            Dictionary<string, decimal> seats,
            string holdToken)
        {
            _logger.LogInformation(
                "Booking {SeatCount} seat(s) for {EventKey} (hold token supplied: {HasHoldToken}).",
                seats.Count, eventKey, !string.IsNullOrWhiteSpace(holdToken));

            var seatsToBook = seats
                .Select(s => new ObjectProperties(s.Key, new Dictionary<string, object> { { "salesPoint", "Admin" } }))
                .ToList();

            ChangeObjectStatusResult response = string.IsNullOrWhiteSpace(holdToken)
                ? await _client.Events.BookAsync(eventKey, seatsToBook)
                : await _client.Events.BookAsync(eventKey, seatsToBook, holdToken);

            if (!string.IsNullOrWhiteSpace(holdToken))
            {
                try
                {
                    await _client.HoldTokens.ExpiresInMinutesAsync(holdToken, 0);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Booking succeeded for {EventKey} but releasing the hold token failed.",
                        eventKey);
                }
            }

            return response;
        }

        public async Task<HoldToken> CreateHoldTokenAsync()
        {
            return _options.HoldExpirationInMinutes.HasValue
                ? await _client.HoldTokens.CreateAsync(_options.HoldExpirationInMinutes.Value)
                : await _client.HoldTokens.CreateAsync();
        }

        public async Task<HoldToken> CreateHoldTokenAsync(string eventKey)
        {
            var schedule = _eventScheduleService.GetList(x => x.ExternalEventKey == eventKey && x.Event.Status == EventStatus.Published).FirstOrDefault();

            int? holdExpiration = schedule?.HoldExpirationInMinutes ?? _options.HoldExpirationInMinutes;

            return holdExpiration.HasValue
                ? await _client.HoldTokens.CreateAsync(holdExpiration.Value)
                : await _client.HoldTokens.CreateAsync();
        }

        public async Task<ChangeObjectStatusResult> HoldSeatsAsync(string eventKey, string[] seats, string holdToken)
        {
            return await _client.Events.HoldAsync(eventKey, seats, holdToken);
        }

        public async Task<HoldToken> GetHoldTokenAsync(string holdToken)
        {
            return await _client.HoldTokens.RetrieveAsync(holdToken);
        }

        public async Task<HoldToken?> ReleaseHoldTokenAsync(string holdToken)
        {
            HoldToken? result = null;

            try
            {
                result = await _client.HoldTokens.ExpiresInMinutesAsync(holdToken, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error releasing hold token {HoldToken}.", holdToken);
            }

            return result;
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

        public async Task<ChangeObjectStatusResult> ReleaseBookedSeatsAsync(ReleaseBookedSeatsRequest request)
        {
            return await _client.Events.ReleaseAsync(request.Key, request.Seats.ToArray());
        }

        public async Task<ChangeObjectStatusResult> ReleaseSeatsAsync(
            string eventKey,
            string[] seats,
            string? holdToken = null,
            bool? keepExtraData = null,
            bool? ignoreChannels = null,
            string[]? channelKeys = null)
        {
            return await _client.Events.ReleaseAsync(eventKey, seats, holdToken: holdToken, keepExtraData: keepExtraData, ignoreChannels: ignoreChannels, channelKeys: channelKeys);
        }

        public async Task<Chart?> RetrieveMapChartAsync(string chartKey)
        {
            try
            {
                return await _client.Charts.RetrieveAsync(chartKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to get chart with key '{ChartKey}'.", chartKey);
                return null;
            }
        }

        public async Task<List<Chart>> RetrieveMapChartsAsync()
        {
            try
            {
                IAsyncEnumerable<Chart> charts = _client.Charts.ListAllAsync(null, null, false, false, false, false);
                return await charts.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to get the list of charts.");
                return [];
            }
        }

        public async Task<bool> EventOrSeasonExistsAsync(string key)
        {
            try
            {
                await _client.Events.RetrieveAsync(key);

                return true;
            }
            catch (SeatsioException ex) when (SeatsIoErrorCodes.IsResourceNotFound(ex))
            {
                return false;
            }
        }

        public async Task<bool> ValidateAllSeatsExistAsync(string key, List<string> seats)
        {
            if (seats == null || seats.Count == 0)
            {
                return false;
            }

            try
            {
                var objectInfos = await _client.Events.RetrieveObjectInfosAsync(key, seats.ToArray());

                bool allExist = seats.All(seat => objectInfos.ContainsKey(seat));

                return allExist;
            }
            catch (SeatsioException ex) when (SeatsIoErrorCodes.IsResourceNotFound(ex))
            {
                return false;
            }
        }

        public async Task SetForSaleAsync(string eventKey, List<string> seatKeys, bool forSale)
        {
            var objects = seatKeys.Select(k => new ObjectAndQuantity(k)).ToArray();

            await _client.Events.EditForSaleConfigAsync(
                eventKey,
                forSale: forSale ? objects : null,
                notForSale: forSale ? null : objects);
        }

        public async Task UpdateExtraDataAsync(string eventKey, List<string> seatKeys, Dictionary<string, object> extraData)
        {
            var extraDatas = seatKeys.ToDictionary(
                k => k,
                _ => new Dictionary<string, object>(extraData));

            await _client.Events.UpdateExtraDatasAsync(eventKey, extraDatas);
        }

        public async Task<Dictionary<string, EventReportSummaryItem>> GetSummaryBySectionAsync(string externalKey)
        {
            return await _client.EventReports.SummaryBySectionAsync(externalKey);
        }

        public async Task<Dictionary<string, EventReportSummaryItem>> GetSummaryByZoneAsync(string externalKey)
        {
            return await _client.EventReports.SummaryByZoneAsync(externalKey);
        }

        public async Task<Dictionary<string, EventReportSummaryItem>> GetSummaryByAvailabilityAsync(string externalKey)
        {
            return await _client.EventReports.SummaryByAvailabilityAsync(externalKey);
        }

        public async Task<Dictionary<string, EventReportSummaryItem>> GetSummaryByAvailabilityReasonAsync(string externalKey)
        {
            return await _client.EventReports.SummaryByAvailabilityReasonAsync(externalKey);
        }

        private async Task<ChangeObjectStatusResult> BookSeatsAsync(string key, List<ObjectProperties> seats, string token)
        {
            string bookingToken = "";

            // Try to retrieve hold token, if it doesn't succeed we ignore the error
            try
            {
                if (string.IsNullOrWhiteSpace(token) == false)
                {
                    var holdToken = await _client.HoldTokens.RetrieveAsync(token);

                    if (holdToken is not null && holdToken.ExpiresInSeconds > 0)
                    {
                        bookingToken = holdToken.Token;
                    }
                }
            }
            catch (Exception)
            {
            }

            // TODO: Handle exceptions SeatsioException
            ChangeObjectStatusResult response;

            if (string.IsNullOrWhiteSpace(bookingToken))
            {
                response = await _client.Events.BookAsync(key, seats);
            }
            else
            {
                response = await _client.Events.BookAsync(key, seats, bookingToken);

                // Try to release hold token after booking attempt, if it doesn't succeed we ignore the error
                try
                {
                    await ReleaseHoldTokenAsync(token);
                }
                catch (Exception)
                {
                }
            }

            return response;
        }

        public async Task<Page<StatusChange>> GetStatusChangesAsync(
            string eventKey,
            long? afterId = null,
            int? pageSize = null)
        {
            var lister = _client.Events.StatusChanges(eventKey, sortField: "date", sortDirection: "asc");

            return afterId.HasValue
                ? await lister.PageAfterAsync(afterId.Value, pageSize)
                : await lister.FirstPageAsync(pageSize);
        }

        public async Task<Page<StatusChange>> GetStatusChangesForObjectAsync(
            string eventKey,
            string objectLabel,
            long? afterId = null,
            int? pageSize = null)
        {
            var lister = _client.Events.StatusChangesForObject(eventKey, objectLabel);

            return afterId.HasValue
                ? await lister.PageAfterAsync(afterId.Value, pageSize)
                : await lister.FirstPageAsync(pageSize);
        }
    }
}

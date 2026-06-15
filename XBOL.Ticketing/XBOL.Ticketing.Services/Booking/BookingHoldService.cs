using SeatsioDotNet.HoldTokens;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Event;
using XBOL.Ticketing.Services.Season;

namespace XBOL.Ticketing.Services.Booking
{
    public class BookingHoldService(
        SeasonService _seasonService,
        IBundleRepository _bundleRepository,
        EventScheduleService _eventScheduleService,
        SeatsIoService _seatsIoService)
    {
        public async Task<HoldToken> ProcessSeatHoldAsync(HoldSeatsRequest request)
        {
            var (eventKeys, holdExpirationMinutes) = await GetHoldConfigAsync(request);

            var token = await _seatsIoService.CreateHoldTokenAsync(holdExpirationMinutes);
            await _seatsIoService.HoldSeatsAsync(eventKeys, request.Seats.ToArray(), token.Token);

            return token;
        }

        private async Task<(string[] EventKeys, int? Expiration)> GetHoldConfigAsync(HoldSeatsRequest request)
        {
            return request.SaleType switch
            {
                SaleType.SeasonPass => await GetSeasonConfigAsync(request.Id),
                SaleType.Bundle => await GetBundleConfigAsync(request.Id),
                SaleType.Event => await GetScheduleConfigAsync(request.Id),
                _ => (Array.Empty<string>(), null)
            };
        }

        private async Task<(string[], int?)> GetSeasonConfigAsync(long id)
        {
            var season = await _seasonService.GetByIdAsync(id);
            var keys = season?.ExternalSeasonKey is not null ? new[] { season.ExternalSeasonKey } : Array.Empty<string>();
            return (keys, null);
        }

        private async Task<(string[], int?)> GetBundleConfigAsync(long id)
        {
            var bundle = _bundleRepository.Get(x => x.Id == id, includedProperties: ["BundleEventSchedules.EventSchedule"]).FirstOrDefault();
            if (bundle is null)
            {
                return (Array.Empty<string>(), null);
            }

            if (bundle.BundleType == BundleType.SeasonPass && bundle.ExternalKey is not null)
            {
                return (new[] { bundle.ExternalKey }, bundle.HoldExpirationInMinutes);
            }

            var keys = bundle.BundleEventSchedules?
                .Select(x => x.EventSchedule?.ExternalEventKey)
                .OfType<string>()
                .ToArray() ?? Array.Empty<string>();

            return (keys, bundle.HoldExpirationInMinutes);
        }

        private async Task<(string[], int?)> GetScheduleConfigAsync(long id)
        {
            var schedule = await _eventScheduleService.GetByIdAsync(id);
            var keys = !string.IsNullOrWhiteSpace(schedule?.ExternalEventKey)
                ? new[] { schedule.ExternalEventKey }
                : Array.Empty<string>();

            return (keys, schedule?.HoldExpirationInMinutes);
        }
    }
}

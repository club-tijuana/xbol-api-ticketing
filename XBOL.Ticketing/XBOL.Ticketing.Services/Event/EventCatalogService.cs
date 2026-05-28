using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.DTO.Results;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Queries;

namespace XBOL.Ticketing.Services.Event
{
    public class EventCatalogService(XBOLDbContext dbContext)
    {
        public async Task<PagedResponse<EventCatalogItemDTO>> GetItemsAsync(EventCatalogQueryParams queryParams)
        {
            var venueNames = await LoadVenueNamesAsync();
            var bundles = await LoadBundlesAsync();
            var events = await LoadEventsAsync();
            var eventMedia = await LoadCatalogMediaAsync(events.Select(eventItem => eventItem.Id), SaleType.Event);
            var bundleMedia = await LoadCatalogMediaAsync(bundles.Select(bundle => bundle.Id), SaleType.Bundle);

            var items = events
                .Select(eventItem => MapEvent(eventItem, venueNames, eventMedia))
                .Concat(bundles.Select(bundle => MapBundle(bundle, venueNames, bundleMedia)))
                .Where(item => MatchesCatalogFilters(item, queryParams))
                .ToList();

            return Page(Sort(items, queryParams.SortBy, queryParams.Descending), queryParams.Page, queryParams.PageSize);
        }

        public async Task<PagedResponse<BundleScheduleItemDTO>> GetBundleScheduleItemsAsync(
            long bundleId,
            BundleScheduleQueryParams queryParams)
        {
            var bundleExists = await dbContext.Bundles.AsNoTracking().AnyAsync(bundle => bundle.Id == bundleId);
            if (!bundleExists)
            {
                throw new KeyNotFoundException($"Bundle {bundleId} not found.");
            }

            var venueNames = await LoadVenueNamesAsync();
            var links = await dbContext.BundleEventSchedules
                .AsNoTracking()
                .AsSplitQuery()
                .Include(link => link.EventSchedule)
                    .ThenInclude(schedule => schedule.Event)
                    .ThenInclude(eventItem => eventItem.VenueMap)
                .Include(link => link.EventSchedule)
                    .ThenInclude(schedule => schedule.Event)
                    .ThenInclude(eventItem => eventItem.Categories)
                .Include(link => link.EventSchedule)
                    .ThenInclude(schedule => schedule.Sections)
                .Where(link => link.BundleId == bundleId)
                .OrderBy(link => link.SortOrder)
                .ToListAsync();

            var items = links
                .Where(link => link.EventSchedule is not null)
                .Select(link => MapBundleSchedule(link.EventSchedule, venueNames))
                .Where(item => MatchesBundleScheduleFilters(item, queryParams))
                .ToList();

            return Page(
                Sort(items, queryParams.SortBy, queryParams.Descending),
                queryParams.Page,
                queryParams.PageSize);
        }

        private async Task<List<Core.Model.Event>> LoadEventsAsync()
        {
            var events = await dbContext.Events
                .AsNoTracking()
                .AsSplitQuery()
                .Include(eventItem => eventItem.VenueMap)
                .Include(eventItem => eventItem.Categories)
                .Include(eventItem => eventItem.Schedules)
                    .ThenInclude(schedule => schedule.Sections)
                .ToListAsync();

            return events
                .Where(eventItem => eventItem is not Core.Model.Bundle)
                .ToList();
        }

        private async Task<List<Core.Model.Bundle>> LoadBundlesAsync()
        {
            return await dbContext.Bundles
                .AsNoTracking()
                .AsSplitQuery()
                .Include(bundle => bundle.VenueMap)
                .Include(bundle => bundle.Categories)
                .Include(bundle => bundle.BundleSections)
                .Include(bundle => bundle.BundleEventSchedules)
                    .ThenInclude(link => link.EventSchedule)
                    .ThenInclude(schedule => schedule.Event)
                    .ThenInclude(eventItem => eventItem.VenueMap)
                .Include(bundle => bundle.BundleEventSchedules)
                    .ThenInclude(link => link.EventSchedule)
                    .ThenInclude(schedule => schedule.Sections)
                .ToListAsync();
        }

        private async Task<Dictionary<long, string>> LoadVenueNamesAsync()
        {
            return await dbContext.VenueMaps
                .AsNoTracking()
                .Select(venueMap => new
                {
                    venueMap.Id,
                    VenueName = venueMap.Venue.Name
                })
                .ToDictionaryAsync(venueMap => venueMap.Id, venueMap => venueMap.VenueName);
        }

        private async Task<Dictionary<long, Dictionary<MediaType, string?>>> LoadCatalogMediaAsync(
            IEnumerable<long> referenceIds,
            SaleType referenceType)
        {
            var ids = referenceIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return [];
            }

            MediaType[] mediaTypes = referenceType == SaleType.Bundle
                ? [MediaType.Banner, MediaType.Logo]
                : [MediaType.Banner];

            var media = await dbContext.Media
                .AsNoTracking()
                .Where(item => item.ReferenceType == referenceType
                    && ids.Contains(item.ReferenceId)
                    && mediaTypes.Contains(item.MediaType))
                .AvailableBlobMedia()
                .OrderBy(item => item.ReferenceId)
                .ThenBy(item => item.MediaType)
                .ThenBy(item => item.Order)
                .ThenBy(item => item.Id)
                .Select(item => new
                {
                    item.ReferenceId,
                    item.MediaType,
                    item.BlobAsset.Url
                })
                .ToListAsync();

            return media
                .GroupBy(item => item.ReferenceId)
                .ToDictionary(
                    referenceGroup => referenceGroup.Key,
                    referenceGroup => referenceGroup
                        .GroupBy(item => item.MediaType)
                        .ToDictionary(
                            mediaTypeGroup => mediaTypeGroup.Key,
                            mediaTypeGroup => mediaTypeGroup.First().Url));
        }

        private static EventCatalogItemDTO MapEvent(
            Core.Model.Event eventItem,
            IReadOnlyDictionary<long, string> venueNames,
            IReadOnlyDictionary<long, Dictionary<MediaType, string?>> media)
        {
            var schedule = PickDisplaySchedule(eventItem.Schedules);
            var bannerImageUrl = MediaUrl(media, eventItem.Id, MediaType.Banner);

            return new EventCatalogItemDTO
            {
                Id = eventItem.Id,
                ItemType = EventCatalogItemType.Event,
                Status = eventItem.Status,
                ScheduledStartDate = schedule?.StartDateTime ?? eventItem.CreatedAt,
                Name = eventItem.Name,
                Categories = Categories(eventItem.Categories),
                VenueMapId = eventItem.VenueMapId,
                VenueName = VenueName(venueNames, eventItem.VenueMapId),
                ExternalEventKey = schedule?.ExternalEventKey,
                AvailableSeats = schedule?.Sections.Sum(section => section.AvailableSeats) ?? 0,
                TotalSeats = schedule?.Sections.Sum(section => section.TotalSeats) ?? 0,
                PosterImageUrl = bannerImageUrl ?? eventItem.PosterImageUrl,
                BannerImageUrl = bannerImageUrl ?? eventItem.BannerImageUrl
            };
        }

        private static EventCatalogItemDTO MapBundle(
            Core.Model.Bundle bundle,
            IReadOnlyDictionary<long, string> venueNames,
            IReadOnlyDictionary<long, Dictionary<MediaType, string?>> media)
        {
            var schedules = BundleSchedules(bundle).ToList();
            var schedule = PickDisplaySchedule(schedules);
            var venueMapId = bundle.VenueMapId ?? schedule?.Event?.VenueMapId;
            var bannerImageUrl = MediaUrl(media, bundle.Id, MediaType.Banner);
            var posterImageUrl = MediaUrl(media, bundle.Id, MediaType.Logo);

            return new EventCatalogItemDTO
            {
                Id = bundle.Id,
                ItemType = EventCatalogItemType.Bundle,
                BundleType = bundle.BundleType,
                Status = bundle.Status,
                ScheduledStartDate = schedule?.StartDateTime ?? bundle.StartDate ?? bundle.CreatedAt,
                Name = bundle.Name,
                Code = bundle.Code,
                Categories = Categories(bundle.Categories),
                VenueMapId = bundle.VenueMapId,
                VenueName = VenueName(venueNames, venueMapId),
                ExternalEventKey = bundle.ExternalKey,
                AvailableSeats = bundle.BundleSections.Sum(section => section.AvailableSeats),
                TotalSeats = bundle.BundleSections.Sum(section => section.TotalSeats),
                PosterImageUrl = posterImageUrl ?? bundle.PosterImageUrl,
                BannerImageUrl = bannerImageUrl ?? bundle.BannerImageUrl,
                IsSeason = bundle.BundleType == BundleType.SeasonPass
            };
        }

        private static string? MediaUrl(
            IReadOnlyDictionary<long, Dictionary<MediaType, string?>> media,
            long referenceId,
            MediaType mediaType)
        {
            return media.TryGetValue(referenceId, out var mediaByType)
                   && mediaByType.TryGetValue(mediaType, out var url)
                ? url
                : null;
        }

        private static BundleScheduleItemDTO MapBundleSchedule(EventSchedule schedule, IReadOnlyDictionary<long, string> venueNames)
        {
            return new BundleScheduleItemDTO
            {
                EventId = schedule.EventId,
                EventScheduleId = schedule.Id,
                ScheduledStartDate = schedule.StartDateTime,
                Name = schedule.Event.Name,
                Categories = Categories(schedule.Event.Categories),
                VenueMapId = schedule.Event.VenueMapId,
                VenueName = VenueName(venueNames, schedule.Event.VenueMapId),
                ExternalEventKey = schedule.ExternalEventKey,
                Status = schedule.Status,
                AvailableSeats = schedule.Sections.Sum(section => section.AvailableSeats),
                TotalSeats = schedule.Sections.Sum(section => section.TotalSeats)
            };
        }

        private static string? VenueName(IReadOnlyDictionary<long, string> venueNames, long? venueMapId)
        {
            return venueMapId is not null && venueNames.TryGetValue(venueMapId.Value, out var venueName)
                ? venueName
                : null;
        }

        private static bool MatchesCatalogFilters(EventCatalogItemDTO item, EventCatalogQueryParams queryParams)
        {
            if (!MatchesSearch(item.Name, queryParams.SearchTerm))
            {
                return false;
            }

            if (queryParams.ItemType is not null && item.ItemType != queryParams.ItemType)
            {
                return false;
            }

            if (queryParams.BundleType is not null && item.BundleType != queryParams.BundleType)
            {
                return false;
            }

            if (queryParams.Status is not null && item.Status != queryParams.Status)
            {
                return false;
            }

            if (!MatchesVenue(item.VenueName, queryParams.Venue))
            {
                return false;
            }

            if (!MatchesDateRange(item.ScheduledStartDate, queryParams.StartDate, queryParams.EndDate))
            {
                return false;
            }

            if (queryParams.Upcoming is null)
            {
                return true;
            }

            return queryParams.Upcoming.Value
                ? item.ScheduledStartDate >= DateTimeOffset.UtcNow
                : item.ScheduledStartDate < DateTimeOffset.UtcNow;
        }

        private static bool MatchesBundleScheduleFilters(BundleScheduleItemDTO item, BundleScheduleQueryParams queryParams)
        {
            return MatchesSearch(item.Name, queryParams.SearchTerm) &&
                   MatchesVenue(item.VenueName, queryParams.Venue) &&
                   MatchesDateRange(item.ScheduledStartDate, queryParams.StartDate, queryParams.EndDate);
        }

        private static bool MatchesSearch(string value, string? searchTerm)
        {
            return string.IsNullOrWhiteSpace(searchTerm) ||
                   value.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesVenue(string? venueName, string? venueFilter)
        {
            return string.IsNullOrWhiteSpace(venueFilter) ||
                   (!string.IsNullOrWhiteSpace(venueName) &&
                    venueName.Contains(venueFilter.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesDateRange(
            DateTimeOffset value,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
        {
            if (startDate is not null && value < startDate.Value)
            {
                return false;
            }

            if (endDate is not null && value > endDate.Value)
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<EventSchedule> BundleSchedules(Core.Model.Bundle bundle)
        {
            return bundle.BundleEventSchedules
                .Select(link => link.EventSchedule)
                .Where(schedule => schedule is not null)
                .Cast<EventSchedule>();
        }

        private static EventSchedule? PickDisplaySchedule(IEnumerable<EventSchedule> schedules)
        {
            var ordered = schedules.OrderBy(schedule => schedule.StartDateTime).ToList();
            return ordered.FirstOrDefault(schedule => schedule.StartDateTime >= DateTimeOffset.UtcNow) ??
                   ordered.LastOrDefault();
        }

        private static List<EventCategoryResult> Categories(IEnumerable<EventCategory> categories)
        {
            return categories.Select(category => new EventCategoryResult
            {
                Id = category.Id,
                Name = category.Name,
                DisplayName = category.DisplayName,
                IsActive = category.IsActive
            }).ToList();
        }

        private static List<T> Sort<T>(IEnumerable<T> items, string? sortBy, bool descending)
            where T : class
        {
            return (NormalizeSort(sortBy), descending, items) switch
            {
                ("name", true, IEnumerable<EventCatalogItemDTO> catalogItems) =>
                    [.. catalogItems.OrderByDescending(item => item.Name).ThenBy(item => item.Id).Cast<T>()],
                ("name", false, IEnumerable<EventCatalogItemDTO> catalogItems) =>
                    [.. catalogItems.OrderBy(item => item.Name).ThenBy(item => item.Id).Cast<T>()],
                ("status", true, IEnumerable<EventCatalogItemDTO> catalogItems) =>
                    [.. catalogItems.OrderByDescending(item => item.Status).ThenBy(item => item.Id).Cast<T>()],
                ("status", false, IEnumerable<EventCatalogItemDTO> catalogItems) =>
                    [.. catalogItems.OrderBy(item => item.Status).ThenBy(item => item.Id).Cast<T>()],
                (_, true, IEnumerable<EventCatalogItemDTO> catalogItems) =>
                    [.. catalogItems.OrderByDescending(item => item.ScheduledStartDate)
                        .ThenBy(item => item.ItemType)
                        .ThenBy(item => item.Id)
                        .Cast<T>()],
                (_, false, IEnumerable<EventCatalogItemDTO> catalogItems) =>
                    [.. catalogItems.OrderBy(item => item.ScheduledStartDate)
                        .ThenBy(item => item.ItemType)
                        .ThenBy(item => item.Id)
                        .Cast<T>()],
                ("name", true, IEnumerable<BundleScheduleItemDTO> scheduleItems) =>
                    [.. scheduleItems.OrderByDescending(item => item.Name).ThenBy(item => item.EventScheduleId).Cast<T>()],
                ("name", false, IEnumerable<BundleScheduleItemDTO> scheduleItems) =>
                    [.. scheduleItems.OrderBy(item => item.Name).ThenBy(item => item.EventScheduleId).Cast<T>()],
                (_, true, IEnumerable<BundleScheduleItemDTO> scheduleItems) =>
                    [.. scheduleItems.OrderByDescending(item => item.ScheduledStartDate).ThenBy(item => item.EventScheduleId).Cast<T>()],
                (_, false, IEnumerable<BundleScheduleItemDTO> scheduleItems) =>
                    [.. scheduleItems.OrderBy(item => item.ScheduledStartDate).ThenBy(item => item.EventScheduleId).Cast<T>()],
                _ => [.. items]
            };
        }

        private static string NormalizeSort(string? sortBy)
        {
            return string.IsNullOrWhiteSpace(sortBy)
                ? "startdate"
                : sortBy.Trim().Replace(" ", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        }

        private static PagedResponse<T> Page<T>(IReadOnlyCollection<T> items, int page, int pageSize)
        {
            var normalizedPage = Math.Max(page, 1);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

            return new PagedResponse<T>
            {
                Items = [.. items.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize)],
                TotalCount = items.Count,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }
    }
}

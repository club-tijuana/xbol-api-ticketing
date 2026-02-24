using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Commons.Views;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
    // TODO: Consider splitting this repository into multiple repositories (e.g., EventScheduleRepository, EventSeatRepository)
    // if it grows too large or if there are distinct areas of responsibility that can be separated for better maintainability.

    public class EventRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Event>(dbContext)
    {
        public async Task<IList<DynamicPricingEvent>> GetDynamicPricingData(long eventId)
        {
            // TODO: Implement logic to exclude EventSeats that have already been delivered to a distributor.
            return await dbContext.EventSchedules
                                  .AsNoTracking()
                                  .AsSplitQuery()
                                  .Where(es => es.EventId == eventId)
                                  .Select(es => new DynamicPricingEvent
                                  {
                                      EventScheduleId = es.Id,
                                      VenueCategory = es.Event.VenueMap.Venue.Category,
                                      VenueLatitude = es.Event.VenueMap.Venue.Latitude,
                                      VenueLongitude = es.Event.VenueMap.Venue.Longitude,
                                      VenueCapacity = es.Event.VenueMap.Capacity,

                                      EventCategory = es.Event.Category,
                                      EventDateTime = es.StartDateTime,
                                      EventPublishedDate = es.PublishedDate,
                                      EventGameCategory = es.GameCategory,

                                      EventProfitability = ProfitabilityType.Regular,
                                      FeelingOfTheMarket = FeelingOfTheMarket.Neutral,

                                      Seats = es.Sections
                                          .SelectMany(s => s.EventSeats)
                                          .Select(seat => new DynamicPricingSeat
                                          {
                                              SeatId = seat.Id,
                                              SeatZone = seat.BaseSeat.BaseRow.BaseSection.BaseZone.Name,
                                              SeatSection = seat.BaseSeat.BaseRow.BaseSection.Name,
                                              SeatRow = seat.BaseSeat.BaseRow.RowLabel,
                                              SeatNumber = seat.BaseSeat.SeatNumber,
                                              SeatType = seat.BaseSeat.SeatType,

                                              SectionBasePrice = seat.EventSection.Price,
                                              IsSold = seat.Tickets.Any(t => t.OriginalOrder != null && t.OriginalOrder.Status == OrderStatus.Paid)
                                          })
                                          .ToList()
                                  })
                                  .ToListAsync();
        }

        public async Task<(List<EventListItem> Items, int TotalCount)> GetEventListAsync(
            List<string>? venues = null,
            List<EventCategory>? categories = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            string? search = null,
            string sortBy = "dateTime",
            bool descending = false,
            int page = 1,
            int pageSize = 20
        )
        {
            var query =
                from e in dbContext.Events
                join es in dbContext.EventSchedules on e.Id equals es.EventId
                where e.Status != EventStatus.Cancelled
                select new
                {
                    Event = e,
                    Schedule = es,
                };

            // Filters
            if (venues?.Count > 0)
            {
                query = query.Where(x => venues.Contains(x.Event.VenueMap.Venue.Name));
            }

            if (categories?.Count > 0)
            {
                query = query.Where(x => categories.Contains(x.Event.Category));
            }

            if (startDate.HasValue)
            {
                query = query.Where(x =>
                    x.Schedule.StartDateTime >= startDate.Value.ToUniversalTime()
                );
            }

            if (endDate.HasValue)
            {
                query = query.Where(x =>
                    x.Schedule.StartDateTime <= endDate.Value.ToUniversalTime()
                );
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(x =>
                    x.Event.Name.ToLower().Contains(term)
                    || x.Schedule.ExternalEventKey.ToLower().Contains(term)
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Sorting
            query = sortBy.ToLower() switch
            {
                "name" => descending
                    ? query.OrderByDescending(x => x.Event.Name)
                    : query.OrderBy(x => x.Event.Name),
                _ => descending
                    ? query.OrderByDescending(x => x.Schedule.StartDateTime)
                    : query.OrderBy(x => x.Schedule.StartDateTime),
            };

            // Pagination
            var skip = (page - 1) * pageSize;
            var pagedQuery = query.Skip(skip).Take(pageSize);

            // Project to DTO with availability aggregation
            var items = await (
                from q in pagedQuery
                join esec in dbContext.EventSections
                    on q.Schedule.Id equals esec.EventScheduleId
                    into sections
                select new EventListItem
                {
                    Id = q.Event.Id,
                    ScheduledStartDate = q.Schedule.StartDateTime,
                    Name = q.Event.Name,
                    Category = q.Event.Category.ToString(),
                    VenueMapId = q.Event.VenueMapId,
                    VenueName = q.Event.VenueMap.Venue.Name,
                    ExternalEventKey = q.Schedule.ExternalEventKey,
                    TotalSeats = sections.Sum(s => s.TotalSeats),
                    AvailableSeats = sections.Sum(s => s.AvailableSeats),
                }
            ).ToListAsync();

            return (items, totalCount);
        }

        public async Task<string?> GetEventKeyAsync(long eventId)
        {
            return await dbContext.EventSchedules
                .AsNoTracking()
                .Where(sch => sch.EventId == eventId)
                .Select(sch => sch.ExternalEventKey)
                .FirstOrDefaultAsync();
        }

        public async Task<EventListItem?> GetEventByIdAsync(long id)
        {
            var query =
                from e in dbContext.Events
                join es in dbContext.EventSchedules on e.Id equals es.EventId
                where e.Id == id && e.Status != EventStatus.Cancelled
                orderby es.StartDateTime
                select new
                {
                    Event = e,
                    Schedule = es,
                };

            return await (
                from q in query.Take(1)
                join esec in dbContext.EventSections
                    on q.Schedule.Id equals esec.EventScheduleId
                    into sections
                select new EventListItem
                {
                    Id = q.Event.Id,
                    ScheduledStartDate = q.Schedule.StartDateTime,
                    Name = q.Event.Name,
                    Category = q.Event.Category.ToString(),
                    VenueMapId = q.Event.VenueMapId,
                    VenueName = q.Event.VenueMap.Venue.Name,
                    ExternalEventKey = q.Schedule.ExternalEventKey,
                    TotalSeats = sections.Sum(s => s.TotalSeats),
                    AvailableSeats = sections.Sum(s => s.AvailableSeats),
                }
            ).FirstOrDefaultAsync();
        }
    }
}

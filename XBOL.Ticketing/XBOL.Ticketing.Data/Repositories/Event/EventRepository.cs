using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
    public class EventRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Event>(dbContext)
    {
        private readonly XBOLDbContext _context = dbContext;

        public async Task<(List<EventListItem> Items, int TotalCount)> GetEventListAsync(
            List<string>? venues = null,
            List<EventCategory>? categories = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            string? search = null,
            string sortBy = "dateTime",
            bool descending = false,
            int page = 1,
            int pageSize = 20)
        {
            var query = from e in _context.Events
                        join es in _context.EventSchedules on e.Id equals es.EventId
                        join vm in _context.VenueMaps on e.VenueMapId equals vm.Id
                        join v in _context.Venues on vm.VenueId equals v.Id
                        where e.Status != EventStatus.Cancelled
                        select new { Event = e, Schedule = es, Venue = v };

            // Filters
            if (venues?.Count > 0)
                query = query.Where(x => venues.Contains(x.Venue.Name));

            if (categories?.Count > 0)
                query = query.Where(x => categories.Contains(x.Event.Category));

            if (startDate.HasValue)
                query = query.Where(x => x.Schedule.StartDateTime >= startDate.Value.ToUniversalTime());

            if (endDate.HasValue)
                query = query.Where(x => x.Schedule.StartDateTime <= endDate.Value.ToUniversalTime());

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(x =>
                    x.Event.Name.ToLower().Contains(term) ||
                    x.Schedule.ExternalEventKey.ToLower().Contains(term));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Sorting
            query = sortBy.ToLower() switch
            {
                "name" => descending ? query.OrderByDescending(x => x.Event.Name) : query.OrderBy(x => x.Event.Name),
                _ => descending ? query.OrderByDescending(x => x.Schedule.StartDateTime) : query.OrderBy(x => x.Schedule.StartDateTime)
            };

            // Pagination
            var skip = (page - 1) * pageSize;
            var pagedQuery = query.Skip(skip).Take(pageSize);

            // Project to DTO with availability aggregation
            var items = await (
                from q in pagedQuery
                join esec in _context.EventSections on q.Schedule.Id equals esec.EventScheduleId into sections
                select new EventListItem
                {
                    Id = q.Event.Id,
                    DateTime = q.Schedule.StartDateTime,
                    TicketIdentifier = q.Schedule.ExternalEventKey,
                    Name = q.Event.Name,
                    Category = q.Event.Category.ToString(),
                    VenueName = q.Venue.Name,
                    TotalSeats = sections.Sum(s => s.TotalSeats),
                    AvailableSeats = sections.Sum(s => s.AvailableSeats)
                }
            ).ToListAsync();

            return (items, totalCount);
        }
    }
}

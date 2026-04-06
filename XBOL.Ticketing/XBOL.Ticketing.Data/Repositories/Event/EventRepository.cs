using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Commons.Views;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
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

                                      EventCategory = es.Event.Categories.Select(c => c.DisplayName).FirstOrDefault() ?? "",
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
        public async Task<string?> GetEventKeyAsync(long eventId)
        {
            return await dbContext.EventSchedules
                .AsNoTracking()
                .Where(sch => sch.EventId == eventId)
                .Select(sch => sch.ExternalEventKey)
                .FirstOrDefaultAsync();
        }
    }
}

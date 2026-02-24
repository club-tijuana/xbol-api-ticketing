using XBOL.Ticketing.Core.Commons.Extensions;
using XBOL.Ticketing.Core.Commons.Views;
using XBOL.Ticketing.DynamicPricing;
using XBOL.Ticketing.DynamicPricing.Models;
using XBOL.Ticketing.DynamicPricing.Models.Catalogs;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.Services.RulesEngine
{
    public class RulesEngineService(
        EventService eventService,
        PriceRuleService priceRuleService,
        EventSeatService eventSeatService,
        IEngine engine)
    {
        public async Task<DynamicPricingResult> ExecuteDynamicPricingAsync(long eventId)
        {
            IList<DynamicPricingEvent> entities = await eventService.GetDynamicPricingData(eventId);
            IList<Context> contexts = [];
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;

            foreach (DynamicPricingEvent entity in entities)
            {
                IList<DynamicPricingRule> dynamicPricingRules = await priceRuleService.GetRulesByEventScheduleIdAsync(entity.EventScheduleId);
                IList<Rule> rules = [.. dynamicPricingRules.Select(x => new Rule { Id = x.Id, Code = x.Code, Description = x.Description, Expression = x.Expression, Version = 1 })];

                TimeSpan remainingTime = entity.EventDateTime - nowUtc;

                if (entity.Seats is null || entity.Seats.Count == 0)
                {
                    continue;
                }

                int seatsSold = entity.Seats.Count(x => x.IsSold);
                decimal salesPace = CalculateSalesPace(entity.Seats, entity.EventDateTime, entity.EventPublishedDate, nowUtc);

                Signals signals = new()
                {
                    VenueCategory = entity.VenueCategory.GetDescription(),
                    VenueLatitude = entity.VenueLatitude,
                    VenueLongitude = entity.VenueLongitude,
                    VenueCapacity = entity.VenueCapacity ?? 0,

                    EventCategory = entity.EventCategory.GetDescription(),
                    EventDateTime = entity.EventDateTime,
                    EventGameCategory = entity.EventGameCategory.GetDescription()
                };

                Features features = new()
                {
                    TimeToEventInDays = ToIntFloor(remainingTime.TotalDays),
                    TimeToEventInHours = ToIntFloor(remainingTime.TotalHours),
                    CurrentInventory = entity.Seats.Count - seatsSold,
                    SalesPace = salesPace,

                    TimeToEventInMinutes = ToIntFloor(remainingTime.TotalMinutes),

                    EventProfitability = entity.EventProfitability.GetDescription(),
                    FeelingOfTheMarket = entity.FeelingOfTheMarket.GetDescription(),
                    //SeatScore = seat.SeatType.GetDescription(),
                };

                contexts.Add(new Context
                {
                    Signals = signals,
                    Features = features,
                    Rules = rules,
                    RulesToApply = rules,
                    RulesTrace = rules,
                    Catalogs = new ItemLists
                    {
                        Seats = [.. entity.Seats.Where(x=>x.SectionBasePrice.HasValue)
                                                .Select(x => new Seat
                                                {
                                                    SeatId = x.SeatId,
                                                    BasePrice = x.SectionBasePrice.HasValue ? x.SectionBasePrice.Value : 0m,
                                                    SeatCode = $"{x.SeatSection} - {x.SeatRow} - {x.SeatNumber}"
                                                })],
                        Orders = []
                    }
                });
            }

            DynamicPricingResult result = engine.CalculatePrices(contexts[0]);

            List<(long Id, decimal PriceOverride)> newPrices = [.. result.PricedSeats.Select(x => (seatId: x.SeatId, Price: x.FinalPrice))];
            await eventSeatService.UpdatePricesFromList(newPrices);

            return result;
        }

        private static int ToIntFloor(double value)
            => (int)Math.Floor(value);

        private static decimal CalculateSalesPace(
          IList<DynamicPricingSeat> seats,
          DateTimeOffset eventDateTime,
          DateTimeOffset publishDate,
          DateTimeOffset now)
        {
            int totalSeats = seats.Count;
            int soldSeats = seats.Count(s => s.IsSold);

            int totalSaleDays = Math.Max(1, (int)Math.Ceiling((eventDateTime - publishDate).TotalDays));
            int elapsedSaleDays = Math.Max(1, (int)Math.Ceiling((now - publishDate).TotalDays));

            int estimatedSeatsPerDay = Math.Max(1, totalSeats / totalSaleDays);

            return (decimal)soldSeats / (estimatedSeatsPerDay * elapsedSaleDays);
        }
    }
}

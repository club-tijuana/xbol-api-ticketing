using System.Collections.Concurrent;
using XBOL.Ticketing.DynamicPricing.Helpers;
using XBOL.Ticketing.DynamicPricing.Models;

namespace XBOL.Ticketing.DynamicPricing
{
    public interface IEngine
    {
        DynamicPricingResult CalculatePrices(Context context);
    }

    /// <summary>
    /// The Engine is responsible for calculating dynamic prices based on the provided Context.
    /// </summary>
    public class Engine : IEngine
    {
        private const int NUMBER_OF_THREADS = 4;
        private const int DECIMAL_ROUNDING_PLACES = 2;

        protected Dictionary<string, object?> _parameters = new Dictionary<string, object?>();

        public DynamicPricingResult CalculatePrices(Context context)
        {
            ContextHelper.OrderRules(ref context);
            _parameters = ContextHelper.SetParameters(context);
            // TODO: Maybe we should calculate the feature here using the signals and catalogs?

            var results = new ConcurrentBag<PricedSeat>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = NUMBER_OF_THREADS
            };

            // TODO: Log start of dynamic price calculation

            Parallel.ForEach(context.Catalogs.Seats, parallelOptions, seat =>
            {
                PricedSeat pricedSeat = new PricedSeat
                {
                    SeatId = seat.SeatId,
                    SeatCode = seat.SeatCode,
                    BasePrice = seat.BasePrice,
                    AppliedRules = []
                };

                var calculator = new NCalcHelper(_parameters);

                foreach (var rule in context.Rules)
                {
                    decimal priceAdjustment = 0;

                    try
                    {
                        priceAdjustment = calculator.Calculate(rule.Expression);
                    }
                    catch
                    {
                        // TODO: Log error
                        priceAdjustment = 0;
                    }

                    pricedSeat.AppliedRules.Add(new AppliedRule
                    {
                        RuleCode = rule.Code,
                        PriceAdjustment = priceAdjustment,
                        Expression = rule.Expression
                    });
                }

                List<decimal> adjustments = pricedSeat.AppliedRules
                                            .Where(ar => ar.PriceAdjustment != 0m)
                                            .Select(ar => ar.PriceAdjustment)
                                            .ToList();

                // Calculate final price using multiplicative composition
                decimal finalPrice = Math.Round(
                    adjustments.Aggregate(seat.BasePrice, (total, next) => total * (1m + next)),
                        DECIMAL_ROUNDING_PLACES,
                        MidpointRounding.AwayFromZero);

                pricedSeat.FinalPrice = finalPrice;

                results.Add(pricedSeat);
            });

            // TODO: Log end of dynamic price calculation

            return new DynamicPricingResult
            {
                PricedSeats = results.ToList(),
                CalculatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}

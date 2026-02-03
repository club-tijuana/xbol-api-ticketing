namespace XBOL.Ticketing.DynamicPricing.Models
{
    /// <summary>
    /// A Feature is a measurable input describing the context of the sale that can influence price.
    /// </summary>
    public class Features
    {
        public int TimeToEventInDays { get; set; }
        public int TimeToEventInHours { get; set; }

        // Current needed features
        public decimal CurrentInventory { get; set; } // Percentage of tickets remaining for the event
        public decimal SalesPace { get; set; } // Tickets sold per unit of time (e.g., per day)
        public int TimeToEventInMinutes { get; set; }
        public string EventProfitability { get; set; } = null!; // A custom index indicating profitability potential; Low, Regular, High, Unique, Premium
        public string FeelingOfTheMarket { get; set; } = null!; // Market sentiment analysis result: Conservative, Neutral, Optimist, Aggressive
        public string SeatScore { get; set; } = null!; // e.g., "Aisle", "Center", "Front", "Back", etc. SeatScore = f(viewAngle, distance, rowHeight) GENERAL, FAN, VIP, Business, Suite
        public string WeatherCondition { get; set; } = null!; // e.g., "Sunny", "Rainy", "Snowy", "Cloudy", etc.
    }
}

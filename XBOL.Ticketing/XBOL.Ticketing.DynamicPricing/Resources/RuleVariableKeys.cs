namespace XBOL.Ticketing.DynamicPricing.Resources
{
    public class RuleVariableKeys
    {
        // Signals

        public const string VenueCategory = "Venue_Category";
        public const string VenueLatitude = "Venue_Latitude";
        public const string VenueLongitude = "Venue_Longitude";
        public const string VenueCapacity = "Venue_Capacity";
        public const string EventCategory = "Event_Category";
        public const string EventDateTime = "Event_DateTime";
        public const string EventGameCategory = "Event_GameCategory";
        public const string SeatZone = "Seat_Zone";
        public const string SeatSection = "Seat_Section";
        public const string SeatRow = "Seat_Row";
        public const string SeatNumber = "Seat_Number";
        public const string SeatType = "Seat_Type";
        public const string SeatBasePrice = "Seat_BasePrice";

        // Features

        public const string TimeToEventInDays = "TimeToEvent_InDays";
        public const string TimeToEventInHours = "TimeToEvent_InHours";
        public const string TimeToEventInMinutes = "TimeToEvent_InMinutes";
        public const string CurrentInventory = "Current_Inventory";
        public const string SalesPace = "Sales_Pace";
        public const string EventProfitability = "Event_Profitability";
        public const string FeelingOfTheMarket = "Feeling_OfTheMarket";
        public const string SeatScore = "Seat_Score";
        public const string WeatherCondition = "Weather_Condition";

        // Adjustments

        public const string NeutralAdjustment = "Neutral_Adjustment";
        public const string LightAdjustment = "Light_Adjustment";
        public const string ModerateAdjustment = "Moderate_Adjustment";
        public const string StrongAdjustment = "Strong_Adjustment";
        public const string AggressiveAdjustment = "Aggressive_Adjustment";
        public const string CriticalAdjustment = "Critical_Adjustment";
        public const string MaximumAdjustment = "Maximum_Adjustment";

        // Set by product

        public const string BasePrice = "Base_Price";

        // Variables equivalent to enum definitions

        public const string ProfitabilityLow = "Profitability_Low";
        public const string ProfitabilityRegular = "Profitability_Regular";
        public const string ProfitabilityHigh = "Profitability_High";
        public const string ProfitabilityUnique = "Profitability_Unique";
        public const string ProfitabilityPremium = "Profitability_Premium";

        public const string FeelingOfTheMarketConservative = "FeelingOfTheMarket_Conservative";
        public const string FeelingOfTheMarketNeutral = "FeelingOfTheMarket_Neutral";
        public const string FeelingOfTheMarketOptimist = "FeelingOfTheMarket_Optimist";
        public const string FeelingOfTheMarketAggressive = "FeelingOfTheMarket_Aggressive";

        public const string SeatScoreStadium = "SeatScore_Stadium";
        public const string SeatScoreAccessible = "SeatScore_Accessible";
        public const string SeatScoreVip = "SeatScore_Vip";
    }
}
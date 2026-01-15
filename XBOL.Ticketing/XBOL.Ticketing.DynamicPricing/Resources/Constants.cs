namespace XBOL.Ticketing.DynamicPricing.Resources
{
    public class Constants
    {
        public const decimal NeutralAdjustmentFactor = 0.01m;
        public const decimal LightAdjustmentFactor = 0.02m;
        public const decimal ModerateAdjustmentFactor = 0.03m;
        public const decimal StrongAdjustmentFactor = 0.05m;
        public const decimal AggressiveAdjustmentFactor = 0.08m;
        public const decimal CriticalAdjustmentFactor = 0.13m;
        public const decimal MaximumAdjustmentFactor = 0.21m;

        public const string ProfitabilityLow = "Low";
        public const string ProfitabilityRegular = "Regular";
        public const string ProfitabilityHigh = "High";
        public const string ProfitabilityUnique = "Unique";
        public const string ProfitabilityPremium = "Premium";

        public const string FeelingOfTheMarketConservative = "Conservative";
        public const string FeelingOfTheMarketNeutral = "Neutral";
        public const string FeelingOfTheMarketOptimist = "Optimist";
        public const string FeelingOfTheMarketAggressive = "Aggressive";

        public const string SeatScoreStadium = "Stadium";
        public const string SeatScoreAccessible = "Accessible";
        public const string SeatScoreVip = "Vip";
    }
}
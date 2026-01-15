using XBOL.Ticketing.DynamicPricing.Models;
using XBOL.Ticketing.DynamicPricing.Resources;

namespace XBOL.Ticketing.DynamicPricing.Helpers
{
    /// <summary>
    /// Provides helper methods for managing and manipulating the Context used for dynamic pricing calculations.
    /// </summary>
    public static class ContextHelper
    {
        /// <summary>
        /// We order the rules to apply based on their dependencies found in their expressions.
        /// </summary>
        public static void OrderRules(ref Context Context)
        {
            var orderedRules = new List<Rule>();
            var ruleCodes = Context.RulesTrace.Select(r => r.Code).ToList();

            foreach (var rule in Context.RulesToApply)
            {
                IList<string> requiredRules = ruleCodes.Where(r => rule.Expression.Contains(r)).ToList();

                if (requiredRules.Any())
                {
                    OrderRule(rule, requiredRules, ref orderedRules, ruleCodes, Context.RulesTrace);
                }
                else
                {
                    orderedRules.Add(rule);
                }
            }

            // Set the ordered rules back to the context and also ensure uniqueness or missing rules are handled
            Context.RulesToApply = orderedRules;
        }

        /// <summary>
        /// Recursively orders a rule based on its required rules.
        /// </summary>
        private static void OrderRule(Rule rule, IList<string> requiredRules, ref List<Rule> orderedRules, IList<string> ruleCodes, IList<Rule> rulesTrace)
        {
            foreach (var requiredRuleCode in requiredRules)
            {
                var requiredRule = rulesTrace.First(r => r.Code == requiredRuleCode);

                IList<string> nestedRequiredRules = ruleCodes.Where(r => requiredRule.Expression.Contains(r)).ToList();

                if (nestedRequiredRules.Any())
                {
                    OrderRule(requiredRule, nestedRequiredRules, ref orderedRules, ruleCodes, rulesTrace);
                }
                else
                {
                    if (!orderedRules.Any(r => r.Code == requiredRule.Code))
                    {
                        orderedRules.Add(requiredRule);
                    }
                }
            }
        }

        public static Dictionary<string, object?> SetParameters(Context Context)
        {
            Dictionary<string, object?> parameters = new Dictionary<string, object?>();

            parameters.Add(RuleVariableKeys.AggressiveAdjustment, Constants.AggressiveAdjustmentFactor);
            parameters.Add(RuleVariableKeys.CriticalAdjustment, Constants.CriticalAdjustmentFactor);
            parameters.Add(RuleVariableKeys.LightAdjustment, Constants.LightAdjustmentFactor);
            parameters.Add(RuleVariableKeys.ModerateAdjustment, Constants.ModerateAdjustmentFactor);
            parameters.Add(RuleVariableKeys.NeutralAdjustment, Constants.NeutralAdjustmentFactor);
            parameters.Add(RuleVariableKeys.StrongAdjustment, Constants.StrongAdjustmentFactor);
            parameters.Add(RuleVariableKeys.MaximumAdjustment, Constants.MaximumAdjustmentFactor);

            parameters.Add(RuleVariableKeys.ProfitabilityLow, Constants.ProfitabilityLow);
            parameters.Add(RuleVariableKeys.ProfitabilityRegular, Constants.ProfitabilityRegular);
            parameters.Add(RuleVariableKeys.ProfitabilityHigh, Constants.ProfitabilityHigh);
            parameters.Add(RuleVariableKeys.ProfitabilityUnique, Constants.ProfitabilityUnique);
            parameters.Add(RuleVariableKeys.ProfitabilityPremium, Constants.ProfitabilityPremium);

            parameters.Add(RuleVariableKeys.FeelingOfTheMarketConservative, Constants.FeelingOfTheMarketConservative);
            parameters.Add(RuleVariableKeys.FeelingOfTheMarketNeutral, Constants.FeelingOfTheMarketNeutral);
            parameters.Add(RuleVariableKeys.FeelingOfTheMarketOptimist, Constants.FeelingOfTheMarketOptimist);
            parameters.Add(RuleVariableKeys.FeelingOfTheMarketAggressive, Constants.FeelingOfTheMarketAggressive);

            parameters.Add(RuleVariableKeys.SeatScoreStadium, Constants.SeatScoreStadium);
            parameters.Add(RuleVariableKeys.SeatScoreAccessible, Constants.SeatScoreAccessible);
            parameters.Add(RuleVariableKeys.SeatScoreVip, Constants.SeatScoreVip);

            parameters.Add(RuleVariableKeys.EventCategory, Context.Signals.EventCategory);
            parameters.Add(RuleVariableKeys.EventDateTime, Context.Signals.EventDateTime);
            parameters.Add(RuleVariableKeys.EventGameCategory, Context.Signals.EventGameCategory);

            parameters.Add(RuleVariableKeys.VenueCapacity, Context.Signals.VenueCapacity);
            parameters.Add(RuleVariableKeys.VenueCategory, Context.Signals.VenueCategory);
            parameters.Add(RuleVariableKeys.VenueLatitude, Context.Signals.VenueLatitude);
            parameters.Add(RuleVariableKeys.VenueLongitude, Context.Signals.VenueLongitude);

            parameters.Add(RuleVariableKeys.CurrentInventory, Context.Features.CurrentInventory);
            parameters.Add(RuleVariableKeys.EventProfitability, Context.Features.EventProfitability);
            parameters.Add(RuleVariableKeys.FeelingOfTheMarket, Context.Features.FeelingOfTheMarket);
            parameters.Add(RuleVariableKeys.SalesPace, Context.Features.SalesPace);
            parameters.Add(RuleVariableKeys.SeatScore, Context.Features.SeatScore);
            parameters.Add(RuleVariableKeys.TimeToEventInDays, Context.Features.TimeToEventInDays);
            parameters.Add(RuleVariableKeys.TimeToEventInHours, Context.Features.TimeToEventInHours);
            parameters.Add(RuleVariableKeys.TimeToEventInMinutes, Context.Features.TimeToEventInMinutes);
            parameters.Add(RuleVariableKeys.WeatherCondition, Context.Features.WeatherCondition);

            return parameters;
        }
    }
}
namespace XBOL.Ticketing.DynamicPricing.Helpers
{
    public class NCalcHelper
    {
        private readonly Dictionary<string, object?> _parameters = [];

        public NCalcHelper(Dictionary<string, object?> parameters)
        {
            _parameters = parameters;
        }

        public decimal Calculate(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                // TODO: Log warning about empty formula
                return 0m;
            }

            try
            {
                var expression = new NCalc.Expression(formula)
                {
                    Parameters = _parameters
                };

                var result = expression.Evaluate();

                if (decimal.TryParse(result?.ToString(), out decimal decimalResult))
                {
                    return decimalResult;
                }
                else
                {
                    // TODO: Log warning about failed parsing
                    return 0m;
                }
            }
            catch (Exception)
            {
                // TODO: Log the exception details
                return 0m;
            }
        }
    }
}

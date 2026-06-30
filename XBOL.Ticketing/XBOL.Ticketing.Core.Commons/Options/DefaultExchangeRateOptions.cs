using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XBOL.Ticketing.Core.Commons.Options
{
    public class DefaultExchangeRateOptions
    {
        [Required]
        [Description("Default exchange rate value used as a fallback for organizers without a configured exchange rate.")]
        public decimal Value { get; set; }
    }
}

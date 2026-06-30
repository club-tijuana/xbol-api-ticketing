using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XBOL.Ticketing.Core.Commons.Options
{
    public class PaymentLinkOptions
    {
        [Required]
        [Description("Frontend URL where clients complete the payment")]
        public string Url { get; set; } = "";
    }
}

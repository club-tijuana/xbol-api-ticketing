using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XBOL.Ticketing.Services.EvoPayment
{
    public class EvoSettings
    {
        [Required]
        [Description("API password provided by EVO Payments")]
        public string APIPassword { get; set; } = string.Empty;

        [Required]
        [Description("Merchant identifier assigned by EVO Payments")]
        public string MerchantId { get; set; } = string.Empty;

        [Required]
        [Description("REST API version of the EVO Payments gateway")]
        public string Version { get; set; } = string.Empty;
    }
}

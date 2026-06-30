using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XBOL.Ticketing.Core.Commons.Options;

public class SeatsIoOptions
{
    [Required]
    [Description("Seats.io workspace secret key")]
    public string SecretKey { get; set; } = string.Empty;

    [Description("Seats.io API region identifier (e.g. NA, EU, SA, OC)")]
    [DefaultValue("NA")]
    public string Region { get; set; } = "NA";

    [Description("Hold token expiration in minutes. When null, uses the default configured in the Seats.io dashboard.")]
    [Range(1, 1440)]
    public int? HoldExpirationInMinutes { get; set; }
}

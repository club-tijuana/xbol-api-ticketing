using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XBOL.Ticketing.API.Options;

public sealed class BackgroundJobDiagnosticsOptions
{
    [Required]
    [Description("Shared key required in the X-XBOL-Diagnostics-Key header for internal background-job diagnostic probes")]
    public string SharedSecret { get; set; } = "";
}

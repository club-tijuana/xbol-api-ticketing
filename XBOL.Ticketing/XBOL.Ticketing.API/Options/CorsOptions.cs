using System.ComponentModel;

namespace XBOL.Ticketing.API.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string DefaultPolicyName = "XBOLPolicy";

    [DefaultValue(DefaultPolicyName)]
    [Description("CORS policy name registered in the pipeline.")]
    public string? PolicyName { get; set; } = DefaultPolicyName;

    [Description("Origins allowed to call the API (exact match). Leave empty to disable app-level CORS.")]
    public string[] AcceptedOrigins { get; set; } = [];
}

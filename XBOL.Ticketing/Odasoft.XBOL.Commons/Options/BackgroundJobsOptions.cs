using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Options;

public class BackgroundJobsOptions
{
    [Required]
    [Description("PostgreSQL connection string for Hangfire storage")]
    public string ConnectionString { get; set; } = "";

    [DefaultValue(false)]
    [Description("Whether Hangfire should install or upgrade its PostgreSQL schema during application startup")]
    public bool PrepareSchemaIfNecessary { get; set; }

    [DefaultValue(false)]
    [Description("Whether the Hangfire dashboard should prevent job actions such as retrying, deleting, or triggering jobs")]
    public bool DashboardReadOnly { get; set; }

    [Range(0, 100)]
    [DefaultValue(0)]
    [Description("Number of Hangfire worker threads. 0 = auto (ProcessorCount × 2)")]
    public int WorkerCount { get; set; }

    [Range(1, 60)]
    [DefaultValue(5)]
    [Description("Server timeout in minutes before Hangfire considers a server gone")]
    public int ServerTimeoutMinutes { get; set; } = 5;

    [Range(1, 300)]
    [DefaultValue(30)]
    [Description("Graceful shutdown timeout in seconds for in-progress jobs")]
    public int ShutdownTimeoutSeconds { get; set; } = 30;
}

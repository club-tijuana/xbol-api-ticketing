using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Options;

public class BackgroundJobsOptions
{
    [Required]
    [Description("PostgreSQL connection string for Hangfire storage")]
    public string ConnectionString { get; set; } = "";
}

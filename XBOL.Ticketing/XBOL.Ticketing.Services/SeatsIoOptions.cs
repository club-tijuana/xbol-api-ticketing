using System.ComponentModel.DataAnnotations;

namespace XBOL.Ticketing.Services;

public class SeatsIoOptions
{
    [Required]
    public string SecretKey { get; set; } = string.Empty;
}

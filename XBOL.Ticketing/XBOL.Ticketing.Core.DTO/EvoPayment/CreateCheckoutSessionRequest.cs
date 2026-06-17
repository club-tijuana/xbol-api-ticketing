using System.ComponentModel.DataAnnotations;

namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class CreateCheckoutSessionRequest
    {
        [Required]
        public required string ReturnUrl { get; init; }

        [Range(0.01, double.MaxValue, ErrorMessage = "The amount must be greater than zero.")]
        public decimal Amount { get; init; }

        public string Currency { get; init; } = "MXN";

        public string? Description { get; init; }
    }
}

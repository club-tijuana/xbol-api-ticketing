using XBOL.Ticketing.Core.DTO.EvoPayment;

namespace XBOL.Ticketing.Services.EvoPayment
{
    public interface IEvoPaymentService
    {
        Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request, CancellationToken ct = default);
        Task<RetrieveOrderResponse> RetrieveOrderAsync(string orderRefId, CancellationToken ct = default);
        Task<InitiateCheckoutResponse> InitiateCheckoutAsync(InitiateCheckoutRequest request, CancellationToken ct = default);
        Task<ConfirmCheckoutResponse> ConfirmCheckoutAsync(ConfirmCheckoutRequest request, CancellationToken ct = default);
    }
}

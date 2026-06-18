using XBOL.Ticketing.Core.Commons.Request;

namespace XBOL.Ticketing.Core.Commons.Email
{
    public interface IEmailJob
    {
        Task SendOrderConfirmationAsync(OrderEmailModel model);
    }
}

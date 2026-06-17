using Odasoft.XBOL.Commons.Requests;

namespace Odasoft.XBOL.Commons.Email;

public interface IEmailJob
{
    Task SendTestEmailAsync(TestEmailModel model);

    Task SendOrderConfirmationAsync(OrderEmailModel model);

    Task SendOrderEmailAsync(OrderEmailModel model, string template, bool generateTickets);

    Task SendAdminInvitationEmailAsync(AuthEmailModel model);

    Task SendAdminRegistrationCompletedEmailAsync(EmailModelBase model);

    Task SendPasswordResetEmailAsync(AuthEmailModel model);
    Task SendEventSubmittedEmailAsync(EventManagementEmailModel model);
    Task SendEventApprovedEmailAsync(EventManagementEmailModel model);
    Task SendEventChangesRequestedEmailAsync(EventManagementEmailModel model);
    Task SendEventPublishedEmailAsync(EventManagementEmailModel model);
}

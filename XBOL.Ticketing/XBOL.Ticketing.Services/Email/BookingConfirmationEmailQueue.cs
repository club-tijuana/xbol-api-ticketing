using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Odasoft.XBOL.Commons.Email;
using Odasoft.XBOL.Commons.Requests;
using TicketingEmailTemplateOptions = XBOL.Ticketing.Core.Commons.Options.EmailTemplateOptions;
using ModelClient = XBOL.Ticketing.Core.Model.Client;

namespace XBOL.Ticketing.Services.Email;

public class BookingConfirmationEmailQueue(
    IBackgroundJobClient backgroundJobClient,
    BookingEmailModelBuilder emailModelBuilder,
    IOptions<TicketingEmailTemplateOptions> emailTemplateOptions,
    ILogger<BookingConfirmationEmailQueue> logger)
{
    private readonly string _supportEmail = emailTemplateOptions.Value.SupportEmail;

    public async Task EnqueueAsync(
        long orderId,
        ModelClient client,
        CancellationToken cancellationToken = default)
    {
        await EnqueueAsync(
            orderId,
            "buyer",
            client.Email ?? "",
            client.FullName ?? "",
            cancellationToken);

        await EnqueueAsync(
            orderId,
            "seller",
            _supportEmail,
            "Seller",
            cancellationToken);
    }

    private async Task EnqueueAsync(
        long orderId,
        string recipientKind,
        string toAddress,
        string toName,
        CancellationToken cancellationToken)
    {
        OrderEmailModel model;

        try
        {
            model = await emailModelBuilder.BuildAsync(
                orderId, toAddress, toName, "es-MX", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to build {RecipientKind} confirmation email model for order {OrderId}",
                recipientKind,
                orderId);
            return;
        }

        try
        {
            var jobId = backgroundJobClient.Enqueue<IEmailJob>(
                x => x.SendOrderConfirmationAsync(model));
            logger.LogInformation(
                "Enqueued {RecipientKind} confirmation email job {JobId} for order {OrderId}",
                recipientKind,
                jobId,
                orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to enqueue {RecipientKind} confirmation email job for order {OrderId}",
                recipientKind,
                orderId);
        }
    }
}

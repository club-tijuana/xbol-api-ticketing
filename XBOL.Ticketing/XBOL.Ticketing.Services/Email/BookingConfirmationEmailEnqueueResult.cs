namespace XBOL.Ticketing.Services.Email;

public enum BookingConfirmationEmailFailureStage
{
    None,
    ModelBuild,
    Enqueue
}

public sealed record BookingConfirmationEmailEnqueueResult(
    long OrderId,
    string RecipientKind,
    bool Succeeded,
    string? JobId,
    BookingConfirmationEmailFailureStage FailureStage,
    string? ExceptionType)
{
    public static BookingConfirmationEmailEnqueueResult Success(
        long orderId,
        string recipientKind,
        string? jobId)
    {
        return new BookingConfirmationEmailEnqueueResult(
            orderId,
            recipientKind,
            true,
            jobId,
            BookingConfirmationEmailFailureStage.None,
            null);
    }

    public static BookingConfirmationEmailEnqueueResult Failure(
        long orderId,
        string recipientKind,
        BookingConfirmationEmailFailureStage failureStage,
        Exception exception)
    {
        return new BookingConfirmationEmailEnqueueResult(
            orderId,
            recipientKind,
            false,
            null,
            failureStage,
            exception.GetType().Name);
    }
}

public static class BookingConfirmationEmailEnqueueResultFormatter
{
    public static string Format(IEnumerable<BookingConfirmationEmailEnqueueResult> results)
    {
        return string.Join(
            ";",
            results.Select(result => result.Succeeded
                ? $"{result.RecipientKind}:succeeded:{result.JobId}"
                : $"{result.RecipientKind}:failed:{result.FailureStage}:{result.ExceptionType}"));
    }
}

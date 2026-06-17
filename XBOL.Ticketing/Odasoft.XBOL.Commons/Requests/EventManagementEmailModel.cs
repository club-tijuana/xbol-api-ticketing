namespace Odasoft.XBOL.Commons.Requests;

public class EventManagementEmailModel : EmailModelBase
{
    public required string ActionUrl { get; set; }
    public required string EventName { get; set; }
}

using Odasoft.XBOL.Commons.Email;

namespace Odasoft.XBOL.Commons.Requests;

public class EmailModelBase
{
    public required string ToAddress { get; set; }
    public required string ToName { get; set; }
    public required string Subject { get; set; }
    public string Culture { get; set; } = "es-MX";
    public EmailTemplateOptions Theme { get; set; } = new();
}

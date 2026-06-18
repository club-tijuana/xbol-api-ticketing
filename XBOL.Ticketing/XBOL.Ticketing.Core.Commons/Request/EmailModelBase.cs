using XBOL.Ticketing.Core.Commons.Options;

namespace XBOL.Ticketing.Core.Commons.Request
{
    public class EmailModelBase
    {
        public string ToAddress { get; set; }
        public string ToName { get; set; }
        public string Subject { get; set; }
        public string Culture { get; set; } = "es-MX";
        public EmailTemplateOptions Theme { get; set; } = new();
    }
}

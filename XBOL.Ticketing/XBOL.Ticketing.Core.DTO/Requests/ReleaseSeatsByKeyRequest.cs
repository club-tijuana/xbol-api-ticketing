namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class ReleaseSeatsByKeyRequest
    {
        public required string EventKey { get; set; }
        public required List<string> Seats { get; set; }
        public string? HoldToken { get; set; }
        public bool? KeepExtraData { get; set; }
        public bool? IgnoreChannels { get; set; }
        public List<string>? ChannelKeys { get; set; }
    }
}

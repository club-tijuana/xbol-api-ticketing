namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class ReleaseBookedSeatsRequest
    {
        public string Key { get; set; } = "";

        public List<string> Seats { get; set; } = [];
    }
}

namespace XBOL.Ticketing.Services;

public interface ISeatsIoSeasonLifecycleClient
{
    Task CreateSeatsIoSeasonAsync(string chartKey, string seasonKey, string[] eventKeys);

    Task CreateSeatsIoEventsInSeasonAsync(string seasonKey, string[] eventKeys);

    Task DeleteSeatsIoSeasonAsync(string seasonKey);

    Task DeleteSeatsIoEventAsync(string eventKey);

    Task UpdateSeatsIoSeasonAsync(string seasonKey, string name);
}

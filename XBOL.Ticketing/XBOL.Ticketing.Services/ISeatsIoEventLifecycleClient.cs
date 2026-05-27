namespace XBOL.Ticketing.Services;

public interface ISeatsIoEventLifecycleClient
{
    Task CreateSeatsIoEventAsync(string chartKey, string eventKey, string name, DateOnly date);
    Task UpdateSeatsIoEventAsync(string eventKey, string name, DateOnly date);
    Task DeleteSeatsIoEventAsync(string eventKey);
}

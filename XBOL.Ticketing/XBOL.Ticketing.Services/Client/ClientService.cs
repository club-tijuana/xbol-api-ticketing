using XBOL.Ticketing.Data.Repositories.Client;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Client
{
    public class ClientService(ClientRepository repository) : BaseService<ClientRepository, Core.Model.Client>(repository)
    {
    }
}

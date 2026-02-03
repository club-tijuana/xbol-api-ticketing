using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Client;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Client
{
    public class ClientCreditTransactionService(ClientCreditTransactionRepository repository) : BaseService<ClientCreditTransactionRepository, ClientCreditTransaction>(repository)
    {
    }
}

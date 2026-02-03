using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Client
{
    public class ClientCreditTransactionRepository(XBOLDbContext dbContext) : BaseRepository<ClientCreditTransaction>(dbContext)
    {
    }
}

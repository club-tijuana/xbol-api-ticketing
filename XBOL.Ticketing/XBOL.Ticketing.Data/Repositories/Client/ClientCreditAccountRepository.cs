using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Client
{
    public class ClientCreditAccountRepository(XBOLDbContext dbContext) : BaseRepository<ClientCreditAccount>(dbContext)
    {
    }
}

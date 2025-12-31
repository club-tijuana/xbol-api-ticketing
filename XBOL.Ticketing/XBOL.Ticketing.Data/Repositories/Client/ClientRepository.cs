using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Client
{
    public class ClientRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Client>(dbContext)
    {
    }
}

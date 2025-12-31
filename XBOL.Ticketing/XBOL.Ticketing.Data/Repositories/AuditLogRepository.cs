using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories
{
    public class AuditLogRepository(XBOLDbContext dbContext) : BaseRepository<AuditLog>(dbContext)
    {
    }
}

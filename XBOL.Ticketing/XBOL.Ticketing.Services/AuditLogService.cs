using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services
{
    public class AuditLogService(AuditLogRepository repository) : BaseService<AuditLogRepository, AuditLog>(repository)
    {
    }
}

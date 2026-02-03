using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Accreditation
{
    public class AccreditationTypeRepository(XBOLDbContext dbContext) : BaseRepository<AccreditationType>(dbContext)
    {
    }
}

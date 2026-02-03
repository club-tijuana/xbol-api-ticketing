using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Accreditation
{
    public class AccreditationRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Accreditation>(dbContext)
    {
    }
}

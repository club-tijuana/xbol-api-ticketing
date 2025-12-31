using XBOL.Ticketing.Data.Repositories.Accreditation;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Accreditation
{
    public class AccreditationService(AccreditationRepository repository) : BaseService<AccreditationRepository, Core.Model.Accreditation>(repository)
    {
    }
}

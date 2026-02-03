using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Accreditation;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Accreditation
{
    public class AccreditationTypeService(AccreditationTypeRepository repository) : BaseService<AccreditationTypeRepository, AccreditationType>(repository)
    {
    }
}

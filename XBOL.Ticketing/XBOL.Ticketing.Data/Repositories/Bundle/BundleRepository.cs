using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Bundle
{
    public class BundleRepository(XBOLDbContext dbContext)
        : BaseRepository<Core.Model.Bundle>(dbContext), IBundleRepository
    {
    }
}

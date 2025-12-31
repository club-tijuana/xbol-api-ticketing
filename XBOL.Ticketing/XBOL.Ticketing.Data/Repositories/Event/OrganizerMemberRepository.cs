using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
    public class OrganizerMemberRepository(XBOLDbContext dbContext) : BaseRepository<OrganizerMember>(dbContext)
    {
    }
}

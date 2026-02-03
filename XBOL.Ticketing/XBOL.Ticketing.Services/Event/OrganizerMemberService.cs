using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Event
{
    public class OrganizerMemberService(OrganizerMemberRepository repository) : BaseService<OrganizerMemberRepository, OrganizerMember>(repository)
    {
    }
}

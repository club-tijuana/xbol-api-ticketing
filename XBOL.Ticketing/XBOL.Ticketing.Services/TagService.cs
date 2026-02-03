using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services
{
    public class TagService(TagRepository repository) : BaseService<TagRepository, Tag>(repository)
    {
    }
}

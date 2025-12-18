using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.Mappers;
using XBOL.Ticketing.Data.Repositories;

namespace XBOL.Ticketing.Services
{
    public class RoleService(RoleRepository repository)
    {
        public async Task<Role> GetById(Guid id)
        {
            Core.Model.Role data = await repository.GetById(id);
            return data.ToDto();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Repositories.Identity
{
    public class RoleRepository(XBOLDbContext dbContext)
    {
        public async Task<Role> GetById(Guid id) => await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id) ??
                                                                                        throw new Exception($"{nameof(Role)} Not Found");
    }
}
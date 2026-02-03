using XBOL.Ticketing.Data.Repositories.Identity;

namespace XBOL.Ticketing.Services.Identity
{
    public class UserService(UserRepository repository)
    {
        private readonly UserRepository _repository = repository;
    }
}

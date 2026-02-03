using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services
{
    public class DeviceService(DeviceRepository repository) : BaseService<DeviceRepository, Device>(repository)
    {
    }
}

using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface IDeviceOsTypeRepository
    {
        Task<IReadOnlyCollection<DeviceOsTypeRecord>> GetAll();
    }
}

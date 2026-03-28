using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface IDeviceOsTypeService
    {
        Task<IReadOnlyCollection<DeviceOsTypeRecord>> GetAll();
    }
}

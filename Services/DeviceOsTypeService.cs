using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class DeviceOsTypeService : IDeviceOsTypeService
    {
        private readonly IDeviceOsTypeRepository _repository;

        public DeviceOsTypeService(IDeviceOsTypeRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyCollection<DeviceOsTypeRecord>> GetAll() => _repository.GetAll();
    }
}

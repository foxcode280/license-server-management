using LicenseManager.API.DTOs.ProductFeatures;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface IProductFeatureService
    {
        Task<IReadOnlyCollection<ProductFeatureRecord>> GetAll();
        Task<ProductFeatureRecord?> GetById(long id);
        Task<ProductFeatureRecord?> Update(long id, UpdateProductFeatureRequestDto request, long userId);
        Task<bool> Deactivate(long id, long userId);
    }
}

using LicenseManager.API.DTOs.ProductFeatures;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface IProductFeatureRepository
    {
        Task<IReadOnlyCollection<ProductFeatureRecord>> GetAll();
        Task<ProductFeatureRecord?> GetById(long id);
        Task<ProductFeatureRecord?> Update(long id, UpdateProductFeatureRequestDto request, long updatedBy);
        Task<bool> Deactivate(long id, long updatedBy);
    }
}

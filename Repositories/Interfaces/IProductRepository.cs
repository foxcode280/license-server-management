using LicenseManager.API.DTOs.Products;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<IReadOnlyCollection<ProductRecord>> GetAll();
        Task<ProductRecord?> GetById(long id);
        Task<ProductRecord?> Update(long id, UpdateProductRequestDto request, long updatedBy);
        Task<bool> Deactivate(long id, long updatedBy);
    }
}

using LicenseManager.API.DTOs.Products;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface IProductService
    {
        Task<IReadOnlyCollection<ProductRecord>> GetAll();
        Task<ProductRecord?> GetById(long id);
        Task<ProductRecord?> Update(long id, UpdateProductRequestDto request, long userId);
        Task<bool> Deactivate(long id, long userId);
    }
}

using LicenseManager.API.DTOs.Products;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyCollection<ProductRecord>> GetAll() => _repository.GetAll();
        public Task<ProductRecord?> GetById(long id) => _repository.GetById(id);

        public async Task<ProductRecord?> Update(long id, UpdateProductRequestDto request, long userId)
        {
            ValidateUserId(userId);
            return await _repository.Update(id, request, userId);
        }

        public async Task<bool> Deactivate(long id, long userId)
        {
            ValidateUserId(userId);
            return await _repository.Deactivate(id, userId);
        }

        private static void ValidateUserId(long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }
        }
    }
}

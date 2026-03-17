using LicenseManager.API.DTOs.ProductFeatures;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class ProductFeatureService : IProductFeatureService
    {
        private readonly IProductFeatureRepository _repository;

        public ProductFeatureService(IProductFeatureRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyCollection<ProductFeatureRecord>> GetAll() => _repository.GetAll();
        public Task<ProductFeatureRecord?> GetById(long id) => _repository.GetById(id);

        public async Task<ProductFeatureRecord?> Update(long id, UpdateProductFeatureRequestDto request, long userId)
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

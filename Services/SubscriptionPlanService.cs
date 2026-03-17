using LicenseManager.API.DTOs.SubscriptionPlans;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly ISubscriptionPlanRepository _repository;

        public SubscriptionPlanService(ISubscriptionPlanRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyCollection<SubscriptionPlanRecord>> GetAll() => _repository.GetAll();
        public Task<SubscriptionPlanRecord?> GetById(long id) => _repository.GetById(id);

        public async Task<SubscriptionPlanRecord?> Update(long id, UpdateSubscriptionPlanRequestDto request, long userId)
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

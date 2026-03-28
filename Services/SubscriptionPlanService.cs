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

        public async Task<SubscriptionPlanRecord?> Create(CreateSubscriptionPlanRequestDto request, long userId)
        {
            ValidateUserId(userId);
            ValidateRequest(request.PlanName, request.ProductId, request.Mode, request.DurationDays, request.DeviceLimit);
            return await _repository.Create(request, userId);
        }

        public async Task<SubscriptionPlanRecord?> Update(long id, UpdateSubscriptionPlanRequestDto request, long userId)
        {
            ValidateUserId(userId);
            ValidateRequest(request.PlanName, request.ProductId, request.Mode, request.DurationDays, request.DeviceLimit);
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

        private static void ValidateRequest(string planName, long productId, string mode, int durationDays, int deviceLimit)
        {
            if (string.IsNullOrWhiteSpace(planName))
            {
                throw new InvalidOperationException("Plan name is required.");
            }

            if (productId <= 0)
            {
                throw new InvalidOperationException("Product is required.");
            }

            if (string.IsNullOrWhiteSpace(mode))
            {
                throw new InvalidOperationException("Mode is required.");
            }

            if (durationDays < 0)
            {
                throw new InvalidOperationException("Duration days cannot be negative.");
            }

            if (deviceLimit < 0)
            {
                throw new InvalidOperationException("Device limit cannot be negative.");
            }
        }
    }
}

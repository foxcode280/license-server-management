using LicenseManager.API.DTOs.Subscriptions;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _repository;

        public SubscriptionService(ISubscriptionRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyCollection<SubscriptionRecord>> GetAll() => _repository.GetAll();

        public Task<SubscriptionRecord?> GetById(long id) => _repository.GetById(id);

        public async Task<SubscriptionRecord?> Create(CreateSubscriptionRequestDto request, long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            if (request.CompanyId <= 0)
            {
                throw new InvalidOperationException("Company is required.");
            }

            if (string.IsNullOrWhiteSpace(request.PlanCategory))
            {
                throw new InvalidOperationException("Plan category is required.");
            }

            if (request.Allocations == null || request.Allocations.Count == 0)
            {
                throw new InvalidOperationException("At least one device allocation is required.");
            }

            if (request.Allocations.Any(x => x.OsTypeId <= 0 || x.AllocatedCount < 0))
            {
                throw new InvalidOperationException("Allocation entries are invalid.");
            }

            var category = request.PlanCategory.Trim().ToUpperInvariant();
            if (category == "DEFAULT" && (!request.PlanId.HasValue || request.PlanId.Value <= 0))
            {
                throw new InvalidOperationException("Default plan selection is required.");
            }

            if (category == "CUSTOM")
            {
                if (!request.ProductId.HasValue || request.ProductId.Value <= 0)
                {
                    throw new InvalidOperationException("Product is required for custom subscriptions.");
                }

                if (!request.DurationDays.HasValue || request.DurationDays.Value <= 0)
                {
                    throw new InvalidOperationException("Duration days is required for custom subscriptions.");
                }
            }

            return await _repository.Create(request, userId);
        }

        public async Task<bool> Reject(long id, long userId, string? reason)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            return await _repository.Reject(id, userId, reason);
        }
    }
}

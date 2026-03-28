using LicenseManager.API.DTOs.SubscriptionPlans;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface ISubscriptionPlanRepository
    {
        Task<IReadOnlyCollection<SubscriptionPlanRecord>> GetAll();
        Task<SubscriptionPlanRecord?> GetById(long id);
        Task<SubscriptionPlanRecord?> Create(CreateSubscriptionPlanRequestDto request, long createdBy);
        Task<SubscriptionPlanRecord?> Update(long id, UpdateSubscriptionPlanRequestDto request, long updatedBy);
        Task<bool> Deactivate(long id, long updatedBy);
    }
}

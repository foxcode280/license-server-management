using LicenseManager.API.DTOs.SubscriptionPlans;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface ISubscriptionPlanService
    {
        Task<IReadOnlyCollection<SubscriptionPlanRecord>> GetAll();
        Task<SubscriptionPlanRecord?> GetById(long id);
        Task<SubscriptionPlanRecord?> Update(long id, UpdateSubscriptionPlanRequestDto request, long userId);
        Task<bool> Deactivate(long id, long userId);
    }
}

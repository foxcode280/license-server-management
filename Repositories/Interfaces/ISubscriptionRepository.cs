using LicenseManager.API.DTOs.Subscriptions;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<IReadOnlyCollection<SubscriptionRecord>> GetAll();
        Task<SubscriptionRecord?> GetById(long id);
        Task<SubscriptionRecord?> Create(CreateSubscriptionRequestDto request, long createdBy);
        Task<bool> Reject(long id, long updatedBy, string? reason);
    }
}

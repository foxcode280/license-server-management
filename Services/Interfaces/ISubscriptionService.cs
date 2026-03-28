using LicenseManager.API.DTOs.Subscriptions;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<IReadOnlyCollection<SubscriptionRecord>> GetAll();
        Task<SubscriptionRecord?> GetById(long id);
        Task<SubscriptionRecord?> Create(CreateSubscriptionRequestDto request, long userId);
        Task<bool> Reject(long id, long userId, string? reason);
    }
}

using LicenseManager.API.DTOs.Users;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<IReadOnlyCollection<UserManagementRecord>> GetAll();
        Task<UserManagementRecord?> GetById(long id);
        Task<UserManagementRecord?> Create(CreateUserRequestDto request, long userId);
        Task<UserManagementRecord?> Update(long id, UpdateUserRequestDto request, long userId);
        Task<bool> Deactivate(long id, long userId);
    }
}

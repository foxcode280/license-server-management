using LicenseManager.API.DTOs.Users;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface IUserManagementRepository
    {
        Task<IReadOnlyCollection<UserManagementRecord>> GetAll();
        Task<UserManagementRecord?> GetById(long id);
        Task<UserManagementRecord?> Create(CreateUserRequestDto request, long createdBy);
        Task<UserManagementRecord?> Update(long id, UpdateUserRequestDto request, long updatedBy);
        Task<bool> Deactivate(long id, long updatedBy);
    }
}

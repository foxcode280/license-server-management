using LicenseManager.API.DTOs.Users;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserManagementRepository _repository;

        public UserManagementService(IUserManagementRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyCollection<UserManagementRecord>> GetAll() => _repository.GetAll();

        public Task<UserManagementRecord?> GetById(long id) => _repository.GetById(id);

        public async Task<UserManagementRecord?> Create(CreateUserRequestDto request, long userId)
        {
            ValidateUserId(userId);
            ValidateRequest(request.Name, request.Email, request.Designation, request.Mobile, request.Role);
            return await _repository.Create(request, userId);
        }

        public async Task<UserManagementRecord?> Update(long id, UpdateUserRequestDto request, long userId)
        {
            ValidateUserId(userId);
            ValidateRequest(request.Name, request.Email, request.Designation, request.Mobile, request.Role);
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

        private static void ValidateRequest(string name, string email, string designation, string mobile, string role)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Name is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(designation))
            {
                throw new InvalidOperationException("Designation is required.");
            }

            if (string.IsNullOrWhiteSpace(mobile))
            {
                throw new InvalidOperationException("Primary mobile is required.");
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new InvalidOperationException("Role is required.");
            }
        }
    }
}

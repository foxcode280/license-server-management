using LicenseManager.API.DTOs.Companies;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _repository;

        public CompanyService(ICompanyRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyCollection<CompanyRecord>> GetAll() => _repository.GetAll();

        public Task<CompanyRecord?> GetById(long id) => _repository.GetById(id);

        public async Task<CompanyRecord?> Create(CreateCompanyRequestDto request, long userId)
        {
            ValidateUserId(userId);
            ValidateRequest(request.Name, request.Industry, request.ContactPerson, request.PrimaryMobile, request.Email, request.Status);
            return await _repository.Create(request, userId);
        }

        public async Task<CompanyRecord?> Update(long id, UpdateCompanyRequestDto request, long userId)
        {
            ValidateUserId(userId);
            ValidateRequest(request.Name, request.Industry, request.ContactPerson, request.PrimaryMobile, request.Email, request.Status);
            return await _repository.Update(id, request, userId);
        }

        public Task<CompanyDetailsResponse?> GetDetails(long id) => _repository.GetDetails(id);

        public async Task<bool> Ban(long id, long userId, string? reason)
        {
            ValidateUserId(userId);
            return await _repository.Ban(id, userId, reason);
        }

        private static void ValidateUserId(long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }
        }

        private static void ValidateRequest(string name, string industry, string contactPerson, string primaryMobile, string email, string status)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Company name is required.");
            }

            if (string.IsNullOrWhiteSpace(industry))
            {
                throw new InvalidOperationException("Industry is required.");
            }

            if (string.IsNullOrWhiteSpace(contactPerson))
            {
                throw new InvalidOperationException("Contact person is required.");
            }

            if (string.IsNullOrWhiteSpace(primaryMobile))
            {
                throw new InvalidOperationException("Primary mobile is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                throw new InvalidOperationException("Status is required.");
            }
        }
    }
}

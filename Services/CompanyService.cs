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

        public async Task<CompanyRecord?> Update(long id, UpdateCompanyRequestDto request, long userId)
        {
            ValidateUserId(userId);
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
    }
}

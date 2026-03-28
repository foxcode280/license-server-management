using LicenseManager.API.DTOs.Companies;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<IReadOnlyCollection<CompanyRecord>> GetAll();
        Task<CompanyRecord?> GetById(long id);
        Task<CompanyRecord?> Create(CreateCompanyRequestDto request, long userId);
        Task<CompanyRecord?> Update(long id, UpdateCompanyRequestDto request, long userId);
        Task<CompanyDetailsResponse?> GetDetails(long id);
        Task<bool> Ban(long id, long userId, string? reason);
    }
}

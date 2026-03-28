using LicenseManager.API.DTOs.Companies;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface ICompanyRepository
    {
        Task<IReadOnlyCollection<CompanyRecord>> GetAll();
        Task<CompanyRecord?> GetById(long id);
        Task<CompanyRecord?> Create(CreateCompanyRequestDto request, long createdBy);
        Task<CompanyRecord?> Update(long id, UpdateCompanyRequestDto request, long updatedBy);
        Task<CompanyDetailsResponse?> GetDetails(long id);
        Task<bool> Ban(long id, long updatedBy, string? reason);
    }
}

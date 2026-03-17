using LicenseManager.API.DTOs.Companies;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface ICompanyRepository
    {
        Task<IReadOnlyCollection<CompanyRecord>> GetAll();
        Task<CompanyRecord?> GetById(long id);
        Task<CompanyRecord?> Update(long id, UpdateCompanyRequestDto request, long updatedBy);
        Task<bool> Deactivate(long id, long updatedBy);
    }
}

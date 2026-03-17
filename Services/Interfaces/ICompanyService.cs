using LicenseManager.API.DTOs.Companies;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<IReadOnlyCollection<CompanyRecord>> GetAll();
        Task<CompanyRecord?> GetById(long id);
        Task<CompanyRecord?> Update(long id, UpdateCompanyRequestDto request, long userId);
        Task<bool> Deactivate(long id, long userId);
    }
}

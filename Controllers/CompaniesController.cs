using LicenseManager.API.DTOs.Companies;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LicenseManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyService _service;

        public CompaniesController(ICompanyService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(await _service.GetAll());
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var company = await _service.GetById(id);
                return company == null ? NotFound() : Ok(company);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpGet("{id:long}/details")]
        public async Task<IActionResult> GetDetails(long id)
        {
            try
            {
                var details = await _service.GetDetails(id);
                return details == null ? NotFound() : Ok(details);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyRequestDto request)
        {
            try
            {
                var company = await _service.Create(request, GetLoggedInUserId());
                return company == null ? BadRequest("Unable to create company.") : Ok(company);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCompanyRequestDto request)
        {
            try
            {
                var company = await _service.Update(id, request, GetLoggedInUserId());
                return company == null ? NotFound() : Ok(company);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPatch("{id:long}/ban")]
        public async Task<IActionResult> Ban(long id, [FromBody] BanCompanyRequestDto request)
        {
            try
            {
                var banned = await _service.Ban(id, GetLoggedInUserId(), request.Reason);
                return banned ? Ok(new { id, message = "Company banned successfully." }) : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        private long GetLoggedInUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userId, out var parsedUserId))
            {
                throw new InvalidOperationException("Logged in user id is missing from the JWT token.");
            }

            return parsedUserId;
        }
    }
}

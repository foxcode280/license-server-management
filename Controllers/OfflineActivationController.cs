using LicenseManager.API.DTOs.OfflineActivations;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace LicenseManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/offline-activations")]
    public class OfflineActivationController : ControllerBase
    {
        private readonly IOfflineActivationService _service;

        public OfflineActivationController(IOfflineActivationService service)
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

        [HttpGet("{licenseId:long}")]
        public async Task<IActionResult> GetByLicenseId(long licenseId)
        {
            try
            {
                var row = await _service.GetByLicenseId(licenseId);
                return row == null ? NotFound() : Ok(row);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPost("{licenseId:long}/upload-request")]
        public async Task<IActionResult> UploadRequest(long licenseId, [FromBody] UploadOfflineActivationRequestDto request)
        {
            try
            {
                return Ok(await _service.UploadRequest(licenseId, request, GetLoggedInUserId()));
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

        [HttpPost("{licenseId:long}/validate-request")]
        public async Task<IActionResult> ValidateRequest(long licenseId)
        {
            try
            {
                return Ok(await _service.ValidateRequest(licenseId, GetLoggedInUserId()));
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

        [HttpPost("{licenseId:long}/generate-final-license")]
        public async Task<IActionResult> GenerateFinalLicense(long licenseId)
        {
            try
            {
                return Ok(await _service.GenerateFinalLicense(licenseId, GetLoggedInUserId()));
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

        [HttpGet("{licenseId:long}/download-final-license")]
        public async Task<IActionResult> DownloadFinalLicense(long licenseId)
        {
            try
            {
                var result = await _service.DownloadFinalLicense(licenseId, GetLoggedInUserId());
                return File(Encoding.UTF8.GetBytes(result.Content), "application/octet-stream", result.FileName);
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

        [HttpGet("{licenseId:long}/download-generic-license")]
        public async Task<IActionResult> DownloadGenericLicense(long licenseId)
        {
            try
            {
                var result = await _service.DownloadGenericLicense(licenseId, GetLoggedInUserId());
                return File(Encoding.UTF8.GetBytes(result.Content), "application/octet-stream", result.FileName);
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

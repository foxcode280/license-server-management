using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LicenseManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/device-os-types")]
    public class DeviceOsTypesController : ControllerBase
    {
        private readonly IDeviceOsTypeService _service;
        public DeviceOsTypesController(IDeviceOsTypeService service)
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
    }
}

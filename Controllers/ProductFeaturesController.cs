using LicenseManager.API.DTOs.ProductFeatures;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LicenseManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/product-features")]
    public class ProductFeaturesController : ControllerBase
    {
        private readonly IProductFeatureService _service;

        public ProductFeaturesController(IProductFeatureService service)
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
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var feature = await _service.GetById(id);
                return feature == null ? NotFound() : Ok(feature);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductFeatureRequestDto request)
        {
            try
            {
                var feature = await _service.Update(id, request, GetLoggedInUserId());
                return feature == null ? NotFound() : Ok(feature);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPatch("{id:long}/deactivate")]
        public async Task<IActionResult> Deactivate(long id)
        {
            try
            {
                var deactivated = await _service.Deactivate(id, GetLoggedInUserId());
                return deactivated ? Ok(new { id, message = "Product feature deactivated successfully." }) : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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

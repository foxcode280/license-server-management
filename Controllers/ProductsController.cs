using LicenseManager.API.DTOs.Products;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LicenseManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
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
                var product = await _service.GetById(id);
                return product == null ? NotFound() : Ok(product);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductRequestDto request)
        {
            try
            {
                var product = await _service.Update(id, request, GetLoggedInUserId());
                return product == null ? NotFound() : Ok(product);
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

        [HttpPatch("{id:long}/deactivate")]
        public async Task<IActionResult> Deactivate(long id)
        {
            try
            {
                var deactivated = await _service.Deactivate(id, GetLoggedInUserId());
                return deactivated ? Ok(new { id, message = "Product deactivated successfully." }) : NotFound();
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

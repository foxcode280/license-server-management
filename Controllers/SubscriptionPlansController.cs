using LicenseManager.API.DTOs.SubscriptionPlans;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LicenseManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/subscription-plans")]
    public class SubscriptionPlansController : ControllerBase
    {
        private readonly ISubscriptionPlanService _service;

        public SubscriptionPlansController(ISubscriptionPlanService service)
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
                var plan = await _service.GetById(id);
                return plan == null ? NotFound() : Ok(plan);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateSubscriptionPlanRequestDto request)
        {
            try
            {
                var plan = await _service.Update(id, request, GetLoggedInUserId());
                return plan == null ? NotFound() : Ok(plan);
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
                return deactivated ? Ok(new { id, message = "Subscription plan deactivated successfully." }) : NotFound();
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

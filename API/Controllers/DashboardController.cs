using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy thống kê tổng quan (Admin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDashboard()
            => Ok(await _service.GetDashboard());

        /// <summary>
        /// Lấy thống kê phòng ban (Manager)
        /// </summary>
        [HttpGet("manager")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetManagerDashboard()
        {
            var userIdClaim = User.FindFirst("id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _service.GetManagerDashboard(userId);
            return Ok(result);
        }
    }
}
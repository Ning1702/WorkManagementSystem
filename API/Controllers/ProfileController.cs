using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _service;
        private readonly IAuthService _authService;  // ✅ thêm

        public ProfileController(IProfileService service, IAuthService authService)  // ✅ thêm
        {
            _service = service;
            _authService = authService;  // ✅ thêm
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst("id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var profile = await _service.GetProfile(userId);
            if (profile == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ." });

            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileDto dto)
        {
            var userIdClaim = User.FindFirst("id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _service.UpdateProfile(userId, dto);

            if (result != "Cập nhật thành công!")
                return BadRequest(new { message = result });

            // ✅ Lấy username từ token hiện tại để tạo token mới
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (username != null)
            {
                // ✅ Tạo token mới với thông tin đã cập nhật
                var newToken = await _authService.RefreshToken(userId);
                return Ok(new { message = result, token = newToken });
            }

            return Ok(new { message = result });
        }
    }
}
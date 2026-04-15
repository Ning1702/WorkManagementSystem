using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        /// <summary>
        /// Đăng ký tài khoản (chờ Admin duyệt)
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(AuthDto dto)
            => Ok(await _service.Register(dto));

        /// <summary>
        /// Đăng nhập và lấy JWT token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
            => Ok(await _service.Login(dto.Username, dto.Password));

        /// <summary>
        /// Đặt lại mật khẩu — CHỈ ADMIN mới được phép
        /// </summary>
        [HttpPost("reset-password")]
        [Authorize(Roles = "Admin")]  // ✅ SỬA: Thêm bảo mật — chỉ Admin reset được
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
            => Ok(await _service.ResetPassword(dto));

        /// <summary>
        /// Lấy danh sách tài khoản chờ duyệt (Admin)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingUsers()
            => Ok(await _service.GetPendingUsers());

        /// <summary>
        /// Duyệt tài khoản (Admin)
        /// </summary>
        [HttpPost("approve/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveUser(Guid userId)
            => Ok(await _service.ApproveUser(userId));

        /// <summary>
        /// Từ chối tài khoản (Admin)
        /// </summary>
        [HttpDelete("reject/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectUser(Guid userId)
            => Ok(await _service.RejectUser(userId));
    }
}

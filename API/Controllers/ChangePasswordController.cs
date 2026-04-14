using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/change-password")]
    public class ChangePasswordController : ControllerBase
    {
        private readonly IChangePasswordService _service;

        public ChangePasswordController(IChangePasswordService service)
        {
            _service = service;
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("id")!.Value);
            var result = await _service.ChangePassword(userId, dto);

            if (result == "Đổi mật khẩu thành công!")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }
    }
}
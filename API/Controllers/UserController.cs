using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service) { _service = service; }

        /// <summary>Lấy danh sách người dùng (Admin xem tất cả, Manager xem phòng mình)</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAll()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var idClaim = User.FindFirst("id")?.Value;

            if (role == "Manager" && Guid.TryParse(idClaim, out var managerId))
                return Ok(await _service.GetByManager(managerId));

            return Ok(await _service.GetAll());
        }

        /// <summary>Tìm kiếm nhân viên theo tên/mã NV/role/phòng</summary>
        [HttpGet("search")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Search(
            string? keyword,
            string? role,
            Guid? unitId)
        {
            var result = await _service.Search(keyword ?? "", role, unitId);
            return Ok(result);
        }

        /// <summary>Cập nhật người dùng (chỉ Admin)</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateUserDto dto)
            => Ok(await _service.Update(id, dto));

        /// <summary>Xóa người dùng (chỉ Admin)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.Delete(id);
            return Ok();
        }
    }
}
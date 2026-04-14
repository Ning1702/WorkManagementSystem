using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/units")]
    public class UnitController : ControllerBase
    {
        private readonly IUnitService _service;
        public UnitController(IUnitService service) { _service = service; }

        /// <summary>Lấy danh sách phòng ban công khai (dùng cho trang đăng ký)</summary>
        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublic()
            => Ok(await _service.GetAll());

        /// <summary>Lấy danh sách tất cả đơn vị (Admin + Manager)</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAll());

        /// <summary>Lấy đơn vị của user đang đăng nhập</summary>
        [HttpGet("my-unit")]
        public async Task<IActionResult> GetMyUnit()
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return Unauthorized();
            var unit = await _service.GetMyUnit(userId);
            if (unit == null) return NotFound(new { message = "Bạn chưa thuộc đơn vị nào" });
            return Ok(unit);
        }

        /// <summary>Lấy danh sách thành viên trong đơn vị</summary>
        [HttpGet("{id}/users")]
        public async Task<IActionResult> GetUsers(Guid id)
            => Ok(await _service.GetUsers(id));

        /// <summary>Tạo đơn vị mới (chỉ Admin)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateUnitDto dto)
            => Ok(await _service.Create(dto));

        /// <summary>Cập nhật đơn vị (chỉ Admin)</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, CreateUnitDto dto)
            => Ok(await _service.Update(id, dto));

        /// <summary>Xóa đơn vị (chỉ Admin)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.Delete(id);
            return Ok(new { message = "Deleted successfully" });
        }

        /// <summary>Thêm thành viên vào đơn vị (Admin + Manager)</summary>
        [HttpPost("{id}/members")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] MemberDto dto)
        {
            await _service.AddMember(id, dto.UserId);
            return Ok(new { message = "Đã thêm thành viên!" });
        }

        /// <summary>Xóa thành viên khỏi đơn vị (Admin + Manager)</summary>
        [HttpDelete("{id}/members/{userId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            await _service.RemoveMember(id, userId);
            return Ok(new { message = "Đã xóa thành viên!" });
        }
    }
}
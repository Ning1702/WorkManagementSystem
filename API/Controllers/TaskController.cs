using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tasks")]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _service;
        public TaskController(ITaskService service) { _service = service; }

        /// <summary>Lấy danh sách task (search + filter + pagination)</summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            string? keyword,
            string? status,
            int page = 1,
            int size = 10,
            bool myTasks = false)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var idClaim = User.FindFirst("id")?.Value;
            Guid? userId = null;

            if (myTasks && Guid.TryParse(idClaim, out var parsedId))
                userId = parsedId;

            // ✅ Manager chỉ thấy task của phòng mình
            Guid? managerUnitId = null;
            if (role == "Manager" && Guid.TryParse(idClaim, out var mid))
                managerUnitId = await _service.GetManagerUnitId(mid);

            var result = await _service.Get(keyword ?? "", page, size, status, userId, managerUnitId);
            return Ok(result);
        }

        /// <summary>Tạo task mới (Manager — chỉ cho phòng mình)</summary>
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(CreateTaskDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("id")!.Value);
            var result = await _service.Create(dto, userId);
            return Ok(result);
        }

        /// <summary>Cập nhật task (Manager)</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update(Guid id, CreateTaskDto dto)
            => Ok(await _service.Update(id, dto));

        /// <summary>Xóa task (Manager)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.Delete(id);
            return Ok(new { message = "Deleted successfully" });
        }
    }
}
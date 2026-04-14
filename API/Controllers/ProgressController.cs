using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/progress")]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _service;

        public ProgressController(IProgressService service)
        {
            _service = service;
        }

        /// <summary>
        /// Xem danh sách tiến độ (có phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            int page = 1,
            int size = 10,
            bool myProgress = false) // ✅ thêm myProgress
        {
            Guid? userId = null;
            if (myProgress)
            {
                var idClaim = User.FindFirst("id")?.Value;
                if (Guid.TryParse(idClaim, out var parsedId))
                    userId = parsedId;
            }
            return Ok(await _service.GetAll(page, size, userId));
        }

        /// <summary>
        /// Cập nhật tiến độ công việc
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Update(CreateProgressDto dto)
            => Ok(await _service.Update(dto));
    }
}
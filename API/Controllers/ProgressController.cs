using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Repositories;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/progress")]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _service;
        private readonly IGenericRepository<User> _userRepo;  // ✅ MỚI

        public ProgressController(
            IProgressService service,
            IGenericRepository<User> userRepo)  // ✅ MỚI
        {
            _service = service;
            _userRepo = userRepo;  // ✅ MỚI
        }

        /// <summary>
        /// Xem danh sách tiến độ (có phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            int page = 1,
            int size = 10,
            bool myProgress = false)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var idClaim = User.FindFirst("id")?.Value;
            Guid? userId = null;
            Guid? unitId = null;

            if (myProgress && Guid.TryParse(idClaim, out var parsedId))
                userId = parsedId;

            // ✅ MỚI: Manager chỉ xem progress của phòng mình
            if (role == "Manager" && !myProgress && Guid.TryParse(idClaim, out var mid))
            {
                var manager = await _userRepo.GetByIdAsync(mid);
                unitId = manager?.UnitId;
            }

            return Ok(await _service.GetAll(page, size, userId, unitId));
        }

        /// <summary>
        /// Cập nhật tiến độ công việc
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Update(CreateProgressDto dto)
            => Ok(await _service.Update(dto));
    }
}

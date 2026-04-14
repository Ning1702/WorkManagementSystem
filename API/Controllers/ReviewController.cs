using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize(Roles = "Admin,Manager")]  // ✅ thêm Admin
    [ApiController]
    [Route("api/review")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewController(IReviewService service)
        {
            _service = service;
        }

        /// <summary>
        /// Phê duyệt hoặc từ chối báo cáo (Admin + Manager)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Review(ReviewDto dto)
            => Ok(await _service.Review(dto));
    }
}
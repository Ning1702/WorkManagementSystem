using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy thông báo của user hiện tại
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = Guid.Parse(User.FindFirst("id")!.Value);
            return Ok(await _service.GetMyNotifications(userId));
        }

        /// <summary>
        /// Đếm thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = Guid.Parse(User.FindFirst("id")!.Value);
            return Ok(await _service.GetUnreadCount(userId));
        }

        /// <summary>
        /// Đánh dấu đã đọc
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await _service.MarkAsRead(id);
            return Ok();
        }
    }
}
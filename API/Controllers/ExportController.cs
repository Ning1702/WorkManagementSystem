using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [ApiController]
    [Route("api/export")]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _service;

        public ExportController(IExportService service)
        {
            _service = service;
        }

        /// <summary>
        /// Export danh sách công việc ra Excel
        /// </summary>
        [HttpGet("tasks")]
        public async Task<IActionResult> ExportTasks()
        {
            var bytes = await _service.ExportTasksToExcel();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DanhSachCongViec_{DateTime.Now:ddMMyyy}.xlsx");
        }

        /// <summary>
        /// Export tiến độ công việc ra Excel
        /// </summary>
        [HttpGet("progress")]
        public async Task<IActionResult> ExportProgress()
        {
            var bytes = await _service.ExportProgressToExcel();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TienDoCongViec_{DateTime.Now:ddMMyyy}.xlsx");
        }
    }
}
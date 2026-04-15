using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkManagementSystem.Application.Interfaces;

namespace WorkManagementSystem.API.Controllers
{
    [Authorize]                    // ✅ thêm
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService _uploadService;

        public UploadController(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        /// <summary>
        /// Upload file (ảnh/tài liệu)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, Guid? progressId)
        {
            var result = await _uploadService.UploadAsync(file, progressId);
            return Ok(result);
        }

        /// <summary>
        /// Tải file đính kèm
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var file = await _uploadService.GetFileByIdAsync(id);
            if (file == null) return NotFound("File not found");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
            return File(fileBytes, "application/octet-stream", file.FileName);
        }
    }
}
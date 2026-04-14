using Microsoft.AspNetCore.Http;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Data;

namespace WorkManagementSystem.Application.Services
{
    public class UploadService : IUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;

        public UploadService(IWebHostEnvironment env, AppDbContext context)
        {
            _env = env;
            _context = context;
        }

        public async Task<UploadFileDto> UploadAsync(IFormFile file, Guid? progressId)  // ✅ đổi return type
        {
            if (file == null || file.Length == 0)
                throw new Exception("File is empty");

            var folderPath = Path.Combine(_env.ContentRootPath, "Uploads");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var newFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(folderPath, newFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var upload = new UploadFile
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                FilePath = filePath,
                CreatedAt = DateTime.UtcNow,
                ProgressId = progressId
            };

            _context.UploadFiles.Add(upload);
            await _context.SaveChangesAsync();

            // ✅ Trả về DTO thay vì entity
            return new UploadFileDto
            {
                Id = upload.Id,
                FileName = upload.FileName,
                FilePath = upload.FilePath,
                CreatedAt = upload.CreatedAt,
                ProgressId = upload.ProgressId
            };
        }
    }
}
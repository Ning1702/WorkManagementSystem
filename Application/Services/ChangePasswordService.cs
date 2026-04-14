using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Infrastructure.Data;

namespace WorkManagementSystem.Application.Services
{
    public class ChangePasswordService : IChangePasswordService
    {
        private readonly AppDbContext _context;

        public ChangePasswordService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> ChangePassword(Guid userId, ChangePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return "Mật khẩu mới không khớp!";

            if (dto.NewPassword.Length < 6)
                return "Mật khẩu mới phải có ít nhất 6 ký tự!";

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return "Không tìm thấy người dùng!";

            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                return "Mật khẩu cũ không đúng!";

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return "Đổi mật khẩu thành công!";
        }
    }
}
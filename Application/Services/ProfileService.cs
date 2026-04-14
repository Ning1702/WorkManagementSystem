using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WorkManagementSystem.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;

        public ProfileService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProfileDto?> GetProfile(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            return new ProfileDto
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task<string> UpdateProfile(Guid userId, ProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return "Không tìm thấy người dùng.";

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();
            return "Cập nhật thành công!";
        }
    }
}
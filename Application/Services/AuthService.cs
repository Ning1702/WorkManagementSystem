using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Data;

namespace WorkManagementSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<string> Register(AuthDto dto)
        {
            var exists = await _context.Users
                .AnyAsync(x => x.Username == dto.Username);
            if (exists)
                throw new Exception("Tên đăng nhập đã tồn tại!");

            var count = await _context.Users.CountAsync();
            var employeeCode = $"EMP{(count + 1):D3}";

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                FullName = dto.FullName,
                EmployeeCode = employeeCode,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "User",
                UnitId = dto.UnitId,
                IsApproved = false  // ✅ chờ Admin duyệt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return "Đăng ký thành công! Vui lòng chờ Admin phê duyệt.";
        }

        public async Task<string> Login(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == username)
                ?? throw new Exception("Tài khoản không tồn tại!");

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new Exception("Mật khẩu không đúng!");

            if (!user.IsApproved)
                throw new Exception("Tài khoản chưa được Admin phê duyệt!");

            return GenerateToken(user);
        }

        public async Task<string> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == dto.Username)
                ?? throw new Exception("Không tìm thấy tài khoản!");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return "Đổi mật khẩu thành công!";
        }

        public async Task<string> ApproveUser(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new Exception("Không tìm thấy tài khoản!");
            user.IsApproved = true;
            await _context.SaveChangesAsync();
            return $"Đã duyệt tài khoản {user.FullName}!";
        }

        public async Task<string> RejectUser(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new Exception("Không tìm thấy tài khoản!");
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return $"Đã xóa tài khoản {user.FullName}!";
        }

        public async Task<List<UserDto>> GetPendingUsers()
        {
            return await _context.Users
                .Where(x => x.IsApproved == false)  // ✅ lọc đúng theo IsApproved
                .Select(x => new UserDto
                {
                    Id = x.Id,
                    Username = x.Username ?? "",
                    FullName = x.FullName ?? "",
                    EmployeeCode = x.EmployeeCode ?? "",
                    Role = x.Role ?? "",
                    UnitId = x.UnitId,
                    IsApproved = x.IsApproved
                })
                .ToListAsync();
        }

        public async Task<string> RefreshToken(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new Exception("Không tìm thấy tài khoản!");
            return GenerateToken(user);
        }

        private string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("employeeCode", user.EmployeeCode ?? ""),
                new Claim("fullName", user.FullName ?? ""),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
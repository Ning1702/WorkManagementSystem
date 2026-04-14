namespace WorkManagementSystem.Application.DTOs
{
    public class AuthDto
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;   // ✅ họ tên thật
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";             // ✅ mặc định User
        public Guid? UnitId { get; set; }                      // ✅ chọn phòng
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Username { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
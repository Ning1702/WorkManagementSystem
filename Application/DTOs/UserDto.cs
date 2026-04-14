namespace WorkManagementSystem.Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;      // ✅ thêm
        public string EmployeeCode { get; set; } = string.Empty;  // ✅ thêm
        public string Role { get; set; } = string.Empty;
        public Guid? UnitId { get; set; }
        public bool IsApproved { get; set; }                      // ✅ thêm
    }

    public class UpdateUserDto
    {
        public string Role { get; set; } = string.Empty;
        public Guid? UnitId { get; set; }
    }
}
namespace WorkManagementSystem.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public Guid? UnitId { get; set; }
        public bool IsApproved { get; set; } = false;
        public string? Email { get; set; }        // ✅ thêm
        public string? PhoneNumber { get; set; }  // ✅ thêm
        public ICollection<UserUnit> UserUnits { get; set; } = new List<UserUnit>();
    }
}
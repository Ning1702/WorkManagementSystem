using System.ComponentModel.DataAnnotations;

namespace WorkManagementSystem.Application.DTOs
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống!")]
        [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự!")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự!")]
        public string Description { get; set; } = string.Empty;

        public DateTime? DueDate { get; set; }
        public List<Guid> UserIds { get; set; } = new();
        public List<Guid> UnitIds { get; set; } = new();
    }

    public class TaskAssigneeDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
    }

    public class TaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public List<TaskAssigneeDto> Assignees { get; set; } = new(); // ✅ MỚI
    }
}

namespace WorkManagementSystem.Application.DTOs
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }          // ✅ thêm
        public List<Guid> UserIds { get; set; } = new();
        public List<Guid> UnitIds { get; set; } = new();
    }

    public class TaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }          // ✅ thêm
    }
}
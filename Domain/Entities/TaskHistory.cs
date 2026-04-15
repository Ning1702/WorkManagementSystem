namespace WorkManagementSystem.Domain.Entities
{
    public class TaskHistory
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid ChangedBy { get; set; }       // Ai thay đổi
        public string FieldName { get; set; }     // "Title", "DueDate", "Status"
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}

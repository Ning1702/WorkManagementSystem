namespace WorkManagementSystem.Domain.Entities
{
    public class Unit
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;  // ✅ MỚI: Soft delete
    }
}

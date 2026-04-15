using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Domain.Entities;

namespace WorkManagementSystem.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UserUnit> UserUnits { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskAssignee> TaskAssignees { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<UploadFile> UploadFiles { get; set; }
        public DbSet<ReportReview> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }  // ✅ MỚI: Audit log

        // ✅ MỚI: Global Query Filter — tự động lọc bản ghi đã xóa mềm
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<Unit>().HasQueryFilter(u => !u.IsDeleted);
        }
    }
}

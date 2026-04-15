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
        public DbSet<TaskHistory> TaskHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<Unit>().HasQueryFilter(u => !u.IsDeleted);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Username).IsRequired();
                entity.Property(x => x.PasswordHash).IsRequired();
                entity.Property(x => x.Role).IsRequired();
            });

            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired();
            });

            modelBuilder.Entity<UserUnit>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.UserUnits)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Unit)
                    .WithMany()
                    .HasForeignKey(x => x.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Title).IsRequired();
                entity.Property(x => x.Description).IsRequired();
            });

            modelBuilder.Entity<TaskAssignee>(entity =>
            {
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<Progress>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Description).IsRequired();
            });

            modelBuilder.Entity<UploadFile>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.FileName).IsRequired();
                entity.Property(x => x.FilePath).IsRequired();
            });

            modelBuilder.Entity<ReportReview>(entity =>
            {
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Message).IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TaskHistory>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.FieldName).IsRequired();
            });
        }
    }
}
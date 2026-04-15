using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Repositories;
using TaskStatus = WorkManagementSystem.Domain.Enums.TaskStatus;

namespace WorkManagementSystem.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IGenericRepository<Progress> _progressRepo;
        private readonly IGenericRepository<ReportReview> _reviewRepo;
        private readonly IGenericRepository<TaskItem> _taskRepo;  // ✅ MỚI
        private readonly IGenericRepository<User> _userRepo;       // ✅ MỚI
        private readonly INotificationService _notificationService;

        public ReviewService(
            IGenericRepository<Progress> progressRepo,
            IGenericRepository<ReportReview> reviewRepo,
            IGenericRepository<TaskItem> taskRepo,        // ✅ MỚI
            IGenericRepository<User> userRepo,             // ✅ MỚI
            INotificationService notificationService)
        {
            _progressRepo = progressRepo;
            _reviewRepo = reviewRepo;
            _taskRepo = taskRepo;              // ✅ MỚI
            _userRepo = userRepo;               // ✅ MỚI
            _notificationService = notificationService;
        }

        // ✅ SỬA: Thêm reviewerId + Đồng bộ Task Status + Kiểm tra quyền
        public async Task<ReviewDto> Review(ReviewDto dto, Guid reviewerId)
        {
            var progress = await _progressRepo.GetByIdAsync(dto.ProgressId)
                ?? throw new Exception("Progress not found");

            // ✅ MỚI: Kiểm tra quyền — Manager chỉ duyệt báo cáo phòng mình
            var reviewer = await _userRepo.GetByIdAsync(reviewerId);
            if (reviewer != null && reviewer.Role == "Manager")
            {
                var submitter = await _userRepo.GetByIdAsync(progress.UserId);
                if (submitter != null && reviewer.UnitId != submitter.UnitId)
                    throw new Exception("Bạn không có quyền duyệt báo cáo của phòng khác!");
            }

            progress.Status = dto.Approve ? TaskStatus.Approved : TaskStatus.Rejected;
            _progressRepo.Update(progress);

            await _reviewRepo.AddAsync(new ReportReview
            {
                Id = Guid.NewGuid(),
                ProgressId = dto.ProgressId,
                IsApproved = dto.Approve,
                Comment = dto.Comment,
                ReviewedAt = DateTime.UtcNow
            });

            // ✅ MỚI: Đồng bộ trạng thái Task
            var task = await _taskRepo.GetByIdAsync(progress.TaskId);
            if (task != null)
            {
                if (dto.Approve)
                {
                    // Nếu % >= 100 → đánh dấu Task hoàn thành
                    task.Status = progress.Percent >= 100
                        ? TaskStatus.Approved
                        : TaskStatus.InProgress;
                }
                else
                {
                    // Bị Reject → Task quay về InProgress, nhân viên cần nộp lại
                    task.Status = TaskStatus.InProgress;
                }
                _taskRepo.Update(task);
            }

            await _reviewRepo.SaveAsync();

            // Gửi thông báo cho User
            var message = dto.Approve
                ? $"✅ Báo cáo của bạn đã được phê duyệt!{(string.IsNullOrEmpty(dto.Comment) ? "" : $" Ghi chú: {dto.Comment}")}"
                : $"❌ Báo cáo của bạn bị từ chối!{(string.IsNullOrEmpty(dto.Comment) ? "" : $" Lý do: {dto.Comment}")}";

            await _notificationService.AddNotification(progress.UserId, message);

            return dto;
        }
    }
}

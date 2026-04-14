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
        private readonly INotificationService _notificationService;  // ✅ thêm

        public ReviewService(
            IGenericRepository<Progress> progressRepo,
            IGenericRepository<ReportReview> reviewRepo,
            INotificationService notificationService)  // ✅ thêm
        {
            _progressRepo = progressRepo;
            _reviewRepo = reviewRepo;
            _notificationService = notificationService;  // ✅ thêm
        }

        public async Task<ReviewDto> Review(ReviewDto dto)
        {
            var progress = await _progressRepo.GetByIdAsync(dto.ProgressId)
                ?? throw new Exception("Progress not found");

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

            await _reviewRepo.SaveAsync();

            // ✅ Gửi thông báo cho User
            var message = dto.Approve
                ? $"✅ Báo cáo của bạn đã được phê duyệt!{(string.IsNullOrEmpty(dto.Comment) ? "" : $" Ghi chú: {dto.Comment}")}"
                : $"❌ Báo cáo của bạn bị từ chối!{(string.IsNullOrEmpty(dto.Comment) ? "" : $" Lý do: {dto.Comment}")}";

            await _notificationService.AddNotification(progress.UserId, message);

            return dto;
        }
    }
}
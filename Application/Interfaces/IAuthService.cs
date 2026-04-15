using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Domain.Entities;

namespace WorkManagementSystem.Application.Interfaces
{
    public interface IUploadService
    {
        Task<UploadFileDto> UploadAsync(IFormFile file, Guid? progressId);
        Task<UploadFileDto?> GetFileByIdAsync(Guid id);      // ✅ MỚI
    }

    public interface IAuthService
    {
        Task<string> Register(AuthDto dto);
        Task<string> Login(string username, string password);
        Task<string> ResetPassword(ResetPasswordDto dto);
        Task<string> ApproveUser(Guid userId);
        Task<string> RejectUser(Guid userId);
        Task<List<UserDto>> GetPendingUsers();
        Task<string> RefreshToken(Guid userId);
    }

    public interface ITaskService
    {
        Task<TaskDto> Create(CreateTaskDto dto, Guid userId);
        Task<object> Get(string keyword, int page, int size, string? status, Guid? userId = null, Guid? unitId = null);
        Task<TaskDto> Update(Guid id, CreateTaskDto dto, Guid changedBy);  // ✅ SỬA: thêm changedBy cho Audit Log
        Task Delete(Guid id);
        Task<Guid?> GetManagerUnitId(Guid managerId);
        Task RemindTask(Guid taskId, Guid reminderId);
    }

    public interface IProgressService
    {
        Task<ProgressDto> Update(CreateProgressDto dto);
        Task<object> GetAll(int page, int size, Guid? userId = null, Guid? unitId = null);  // ✅ SỬA: thêm unitId
    }

    public interface IReviewService
    {
        Task<ReviewDto> Review(ReviewDto dto, Guid reviewerId);  // ✅ SỬA: thêm reviewerId
    }

    public interface IUnitService
    {
        Task<List<UnitDto>> GetAll();
        Task<UnitDto?> GetMyUnit(Guid userId);
        Task<List<UserDto>> GetUsers(Guid unitId);
        Task<UnitDto> Create(CreateUnitDto dto);
        Task<UnitDto> Update(Guid id, CreateUnitDto dto);
        Task Delete(Guid id);
        Task AddMember(Guid unitId, Guid userId);
        Task RemoveMember(Guid unitId, Guid userId);
    }

    public interface IUserService
    {
        Task<List<UserDto>> GetAll();
        Task<List<UserDto>> GetByManager(Guid managerId);
        Task<List<UserDto>> Search(string keyword, string? role, Guid? unitId);
        Task<UserDto> Update(Guid id, UpdateUserDto dto);
        Task Delete(Guid id);
        Task<PerformanceDto> GetPerformanceAsync(Guid userId);          // ✅ MỚI: KPI cá nhân
        Task<List<PerformanceDto>> GetUnitPerformanceAsync(Guid managerId); // ✅ MỚI: KPI toàn phòng
    }

    public interface INotificationService
    {
        Task AddNotification(Guid userId, string message);
        Task<List<NotificationDto>> GetMyNotifications(Guid userId);
        Task MarkAsRead(Guid notificationId);
        Task<int> GetUnreadCount(Guid userId);
    }

    public interface IExportService
    {
        Task<byte[]> ExportTasksToExcel();
        Task<byte[]> ExportProgressToExcel();
    }

    public interface IChangePasswordService
    {
        Task<string> ChangePassword(Guid userId, ChangePasswordDto dto);
    }

    public interface IProfileService
    {
        Task<ProfileDto?> GetProfile(Guid userId);
        Task<string> UpdateProfile(Guid userId, ProfileDto dto);
    }

    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboard();
        Task<ManagerDashboardDto> GetManagerDashboard(Guid userId);
    }
}

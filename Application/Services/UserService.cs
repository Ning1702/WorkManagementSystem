using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Repositories;
using TaskStatusEnum = WorkManagementSystem.Domain.Enums.TaskStatus;

namespace WorkManagementSystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IGenericRepository<User> _repo;
        private readonly IGenericRepository<UserUnit> _userUnitRepo;
        private readonly IGenericRepository<TaskItem> _taskRepo;
        private readonly IGenericRepository<TaskAssignee> _assigneeRepo;
        private readonly IGenericRepository<Progress> _progressRepo;
        private readonly IMapper _mapper;

        public UserService(
            IGenericRepository<User> repo,
            IGenericRepository<UserUnit> userUnitRepo,
            IGenericRepository<TaskItem> taskRepo,
            IGenericRepository<TaskAssignee> assigneeRepo,
            IGenericRepository<Progress> progressRepo,
            IMapper mapper)
        {
            _repo = repo;
            _userUnitRepo = userUnitRepo;
            _taskRepo = taskRepo;
            _assigneeRepo = assigneeRepo;
            _progressRepo = progressRepo;
            _mapper = mapper;
        }

        public async Task<List<UserDto>> GetAll()
            => _mapper.Map<List<UserDto>>(await _repo.Query()
                .Where(u => !u.IsDeleted)
                .ToListAsync());

        public async Task<List<UserDto>> GetByManager(Guid managerId)
        {
            var manager = await _repo.GetByIdAsync(managerId);
            if (manager?.UnitId == null) return new List<UserDto>();

            var unitId = manager.UnitId.Value;
            var userIdsFromMapping = await _userUnitRepo.Query()
                .Where(uu => uu.UnitId == unitId)
                .Select(uu => uu.UserId)
                .ToListAsync();

            var users = await _repo.Query()
                .Where(u => (u.UnitId == unitId || userIdsFromMapping.Contains(u.Id))
                            && u.Role != "Admin"
                            && u.IsApproved
                            && !u.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<UserDto>>(users);
        }

        public async Task<List<UserDto>> Search(string keyword, string? role, Guid? unitId)
        {
            var query = _repo.Query().Where(u => u.Role != "Admin");

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.Contains(keyword)) ||
                    (u.EmployeeCode != null && u.EmployeeCode.Contains(keyword)) ||
                    u.Username.Contains(keyword));

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            if (unitId.HasValue)
            {
                var userIdsInUnit = _userUnitRepo.Query()
                    .Where(uu => uu.UnitId == unitId.Value)
                    .Select(uu => uu.UserId);
                query = query.Where(u =>
                    userIdsInUnit.Contains(u.Id) ||
                    u.UnitId == unitId.Value);
            }

            return _mapper.Map<List<UserDto>>(await query.ToListAsync());
        }

        public async Task<UserDto> Update(Guid id, UpdateUserDto dto)
        {
            var user = await _repo.GetByIdAsync(id)
                ?? throw new Exception("User not found");
            user.Role = dto.Role;
            if (dto.Role == "Manager")
                user.UnitId = dto.UnitId;
            else
                user.UnitId = null;
            _repo.Update(user);
            await _repo.SaveAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task Delete(Guid id)
        {
            var user = await _repo.GetByIdAsync(id)
                ?? throw new Exception("User not found");
            if (user.Role == "Admin")
                throw new Exception("Không thể xóa tài khoản Admin!");

            user.IsDeleted = true;
            _repo.Update(user);
            await _repo.SaveAsync();
        }

        /// <summary>
        /// Lấy điểm KPI (Có phân luồng User thường vs Manager)
        /// </summary>
        public async Task<PerformanceDto> GetPerformanceAsync(Guid userId)
        {
            var user = await _repo.GetByIdAsync(userId)
                ?? throw new Exception("User not found");

            var now = DateTime.UtcNow;

            if (user.Role == "Manager")
                return await CalculateManagerPerformanceAsync(userId, user, now);

            return await CalculatePersonalPerformanceDtoAsync(userId, user, now);
        }

        private async Task<PerformanceDto> CalculatePersonalPerformanceDtoAsync(Guid userId, User user, DateTime now)
        {
            // 1. Task được giao
            var assignedTaskIds = await _assigneeRepo.Query()
                .Where(a => a.UserId == userId)
                .Select(a => a.TaskId)
                .ToListAsync();

            var tasks = await _taskRepo.Query()
                .Where(t => assignedTaskIds.Contains(t.Id) && !t.IsDeleted)
                .ToListAsync();

            // 2. Task quá hạn = có deadline, chưa submit/approve, đã qua hạn
            var overdueTasks = tasks
                .Where(t => t.DueDate.HasValue
                         && t.DueDate.Value < now
                         && t.Status != TaskStatusEnum.Approved
                         && t.Status != TaskStatusEnum.Submitted)
                .ToList();

            // 3. Kiểm tra hoàn thành đúng/trễ hạn
            var approvedTaskIds = tasks
                .Where(t => t.Status == TaskStatusEnum.Approved && t.DueDate.HasValue)
                .Select(t => t.Id).ToList();

            var progressList = await _progressRepo.Query()
                .Where(p => p.UserId == userId)
                .ToListAsync();

            int completedOnTime = 0, completedLate = 0;
            foreach (var taskId in approvedTaskIds)
            {
                var task = tasks.First(t => t.Id == taskId);
                var firstProgress = progressList
                    .Where(p => p.TaskId == taskId)
                    .OrderBy(p => p.UpdatedAt)
                    .FirstOrDefault();

                if (firstProgress != null && task.DueDate.HasValue)
                {
                    if (firstProgress.UpdatedAt <= task.DueDate.Value) completedOnTime++;
                    else completedLate++;
                }
            }

            // 4. Báo cáo bị từ chối
            int rejectedCount = progressList.Count(p => p.Status == TaskStatusEnum.Rejected);

            // 5. Tính điểm với phạt lũy tiến
            int bonusPoints = completedOnTime * 5;
            int penaltyPoints = 0;

            // Phạt lũy tiến theo số lần vi phạm: lần 1=-5, lần 2=-8, lần 3+=-12
            for (int i = 0; i < overdueTasks.Count; i++)
                penaltyPoints += i == 0 ? 5 : i == 1 ? 8 : 12;

            penaltyPoints += rejectedCount * 3;
            int score = Math.Max(0, 100 + bonusPoints - penaltyPoints);

            // 6. Xác định cấp độ
            string level, levelColor, levelIcon;
            if (score >= 90)      { level = "Xuất sắc"; levelColor = "green";  levelIcon = "⭐"; }
            else if (score >= 75) { level = "Tốt";      levelColor = "blue";   levelIcon = "✅"; }
            else if (score >= 60) { level = "Trung bình"; levelColor = "yellow"; levelIcon = "⚠️"; }
            else                  { level = "Yếu";      levelColor = "red";    levelIcon = "🔴"; }

            // 7. Cảnh báo
            bool isAtRisk = overdueTasks.Count >= 3 || score < 60;
            string warning = "";
            if (overdueTasks.Count >= 3)
                warning = $"⚠️ Vi phạm {overdueTasks.Count} lần quá hạn! Cần cải thiện ngay.";
            else if (score < 60)
                warning = "⚠️ Điểm hiệu suất thấp! Cần chú ý cải thiện chất lượng công việc.";

            return new PerformanceDto
            {
                UserId = userId,
                FullName = user.FullName ?? "—",
                EmployeeCode = user.EmployeeCode ?? "—",
                Score = score,
                Level = level,
                LevelColor = levelColor,
                LevelIcon = levelIcon,
                TotalTasks = tasks.Count,
                CompletedOnTime = completedOnTime,
                CompletedLate = completedLate,
                OverdueTasks = overdueTasks.Count,
                RejectedReports = rejectedCount,
                BonusPoints = bonusPoints,
                PenaltyPoints = penaltyPoints,
                IsAtRisk = isAtRisk,
                WarningMessage = warning
            };
        }

        private async Task<PerformanceDto> CalculateManagerPerformanceAsync(Guid managerId, User user, DateTime now)
        {
            // 1. KPI Cá nhân (Các task mà Giám đốc giao trực tiếp cho Manager)
            var personalDto = await CalculatePersonalPerformanceDtoAsync(managerId, user, now);
            int personalScore = personalDto.TotalTasks == 0 ? 100 : personalDto.Score;

            // 2. KPI Phòng ban (Bằng trung bình cộng nhân viên trong phòng)
            double unitAvgScore = 100;
            var unitPerformanceList = new List<PerformanceDto>();

            if (user.UnitId.HasValue)
            {
                var unitId = user.UnitId.Value;
                var userIdsFromMapping = await _userUnitRepo.Query()
                    .Where(uu => uu.UnitId == unitId)
                    .Select(uu => uu.UserId)
                    .ToListAsync();

                var memberIds = await _repo.Query()
                    .Where(u => (u.UnitId == unitId || userIdsFromMapping.Contains(u.Id))
                                && u.Role == "User"
                                && u.IsApproved
                                && !u.IsDeleted)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var uid in memberIds)
                {
                    var member = await _repo.GetByIdAsync(uid);
                    if (member != null)
                        unitPerformanceList.Add(await CalculatePersonalPerformanceDtoAsync(uid, member, now));
                }

                if (unitPerformanceList.Count > 0)
                    unitAvgScore = unitPerformanceList.Average(p => p.Score);
            }

            // 3. Phạt ngâm task (SLA Violation): Tiến độ Submitted nhưng Manager chưa duyệt quá 48h
            var memberIdsForReview = unitPerformanceList.Select(p => p.UserId).ToList();
            var pendingProgresses = await _progressRepo.Query()
                .Where(p => memberIdsForReview.Contains(p.UserId) && p.Status == TaskStatusEnum.Submitted)
                .ToListAsync();

            int reviewPenaltyCount = 0;
            foreach (var p in pendingProgresses)
                if ((now - p.UpdatedAt).TotalHours > 48)
                    reviewPenaltyCount++;

            int reviewPenaltyPoints = reviewPenaltyCount * 3; // Nút thắt cổ chai: phạt nặng

            // 4. Tổng kết điểm: 70% đóng góp phòng ban, 30% hoàn thành cá nhân, trừ điểm phạt quản lý
            int finalScore = (int)Math.Round(unitAvgScore * 0.7 + personalScore * 0.3) - reviewPenaltyPoints;
            finalScore = Math.Max(0, finalScore);

            string level, levelColor, levelIcon;
            if (finalScore >= 90)      { level = "Xuất sắc"; levelColor = "green";  levelIcon = "⭐"; }
            else if (finalScore >= 75) { level = "Tốt";      levelColor = "blue";   levelIcon = "✅"; }
            else if (finalScore >= 60) { level = "Trung bình"; levelColor = "yellow"; levelIcon = "⚠️"; }
            else                       { level = "Yếu";      levelColor = "red";    levelIcon = "🔴"; }

            bool isAtRisk = finalScore < 60 || reviewPenaltyCount > 0 || personalDto.IsAtRisk;
            List<string> warnings = new List<string>();
            
            if (reviewPenaltyCount > 0)
                warnings.Add($"⚠️ Nút thắt cổ chai: '{reviewPenaltyCount}' báo cáo bị ngâm chưa duyệt quá 48h!");
            
            if (finalScore < 60)
                warnings.Add("⚠️ Hiệu suất lãnh đạo phòng ban thấp, ảnh hưởng điểm KPI quản lý!");

            if (!string.IsNullOrEmpty(personalDto.WarningMessage))
                warnings.Add(personalDto.WarningMessage);

            string warning = string.Join(" | ", warnings);

            return new PerformanceDto
            {
                UserId = managerId,
                FullName = user.FullName ?? "—",
                EmployeeCode = user.EmployeeCode ?? "—",
                Score = finalScore,
                Level = level,
                LevelColor = levelColor,
                LevelIcon = levelIcon,
                TotalTasks = personalDto.TotalTasks,
                CompletedOnTime = personalDto.CompletedOnTime,
                CompletedLate = personalDto.CompletedLate,
                OverdueTasks = personalDto.OverdueTasks,
                RejectedReports = personalDto.RejectedReports,
                BonusPoints = personalDto.BonusPoints,
                PenaltyPoints = personalDto.PenaltyPoints,
                ReviewPenaltyPoints = reviewPenaltyPoints,
                IsManagerKpi = true,
                UnitAverageScore = unitAvgScore,
                PersonalScore = personalScore,
                IsAtRisk = isAtRisk,
                WarningMessage = warning
            };
        }

        /// <summary>
        /// Lấy bảng KPI toàn phòng cho Manager — sắp xếp theo điểm giảm dần
        /// </summary>
        public async Task<List<PerformanceDto>> GetUnitPerformanceAsync(Guid managerId)
        {
            var manager = await _repo.GetByIdAsync(managerId);
            if (manager?.UnitId == null) return new List<PerformanceDto>();

            var unitId = manager.UnitId.Value;
            var userIdsFromMapping = await _userUnitRepo.Query()
                .Where(uu => uu.UnitId == unitId)
                .Select(uu => uu.UserId)
                .ToListAsync();

            var memberIds = await _repo.Query()
                .Where(u => (u.UnitId == unitId || userIdsFromMapping.Contains(u.Id))
                            && u.Role == "User"
                            && u.IsApproved
                            && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();

            var result = new List<PerformanceDto>();
            foreach (var uid in memberIds)
            {
                var member = await _repo.GetByIdAsync(uid);
                if (member != null)
                    result.Add(await CalculatePersonalPerformanceDtoAsync(uid, member, DateTime.UtcNow));
            }

            return result.OrderByDescending(p => p.Score).ToList();
        }
    }
}

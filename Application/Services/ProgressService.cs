using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Repositories;
using TaskStatus = WorkManagementSystem.Domain.Enums.TaskStatus;

namespace WorkManagementSystem.Application.Services
{
    public class ProgressService : IProgressService
    {
        private readonly IGenericRepository<Progress> _repo;
        private readonly IGenericRepository<TaskItem> _taskRepo;
        private readonly IGenericRepository<TaskAssignee> _assigneeRepo;
        private readonly IGenericRepository<UserUnit> _userUnitRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<UploadFile> _uploadRepo;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public ProgressService(
            IGenericRepository<Progress> repo,
            IGenericRepository<TaskItem> taskRepo,
            IGenericRepository<TaskAssignee> assigneeRepo,
            IGenericRepository<UserUnit> userUnitRepo,
            IGenericRepository<User> userRepo,
            IGenericRepository<UploadFile> uploadRepo,
            INotificationService notificationService,
            IMapper mapper)
        {
            _repo = repo;
            _taskRepo = taskRepo;
            _assigneeRepo = assigneeRepo;
            _userUnitRepo = userUnitRepo;
            _userRepo = userRepo;
            _uploadRepo = uploadRepo;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task<ProgressDto> Update(CreateProgressDto dto)
        {
            var progress = _mapper.Map<Progress>(dto);
            progress.Id = Guid.NewGuid();
            progress.Status = TaskStatus.Submitted;
            progress.UpdatedAt = DateTime.UtcNow;
            await _repo.AddAsync(progress);

            // Cập nhật status của Task → Submitted
            var task = await _taskRepo.GetByIdAsync(dto.TaskId);
            if (task != null)
            {
                task.Status = TaskStatus.Submitted;
                _taskRepo.Update(task);
            }

            await _repo.SaveAsync();

            // Gửi thông báo cho Manager của phòng
            var user = await _userRepo.GetByIdAsync(dto.UserId);
            if (user?.UnitId != null)
            {
                var managers = await _userRepo.Query()
                    .Where(u => u.Role == "Manager" && u.UnitId == user.UnitId)
                    .ToListAsync();
                foreach (var mgr in managers)
                {
                    await _notificationService.AddNotification(mgr.Id,
                        $"Nhân viên {user.FullName} đã gửi báo cáo tiến độ cho công việc: {task?.Title ?? ""}");
                }
            }

            return _mapper.Map<ProgressDto>(progress);
        }

        public async Task<object> GetAll(int page, int size, Guid? userId = null, Guid? unitId = null)
        {
            var query = _repo.Query();

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            if (unitId.HasValue)
            {
                // Lấy user từ bảng liên kết
                var userIdsFromMapping = _userUnitRepo.Query()
                    .Where(uu => uu.UnitId == unitId.Value)
                    .Select(uu => uu.UserId);

                // Lấy cả user có UnitId trực tiếp
                var userIdsFromDirect = _userRepo.Query()
                    .Where(u => u.UnitId == unitId.Value && u.IsApproved && !u.IsDeleted)
                    .Select(u => u.Id);

                query = query.Where(p =>
                    userIdsFromMapping.Contains(p.UserId) ||
                    userIdsFromDirect.Contains(p.UserId));
            }

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(p => p.UpdatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Join(_userRepo.Query(),
                    p => p.UserId,
                    u => u.Id,
                    (p, u) => new { p, u.FullName, u.EmployeeCode })
                .Join(_taskRepo.Query(),
                    x => x.p.TaskId,
                    t => t.Id,
                    (x, t) => new { x.p, x.FullName, x.EmployeeCode, TaskTitle = t.Title })
                .ToListAsync();

            var progressIds = data.Select(x => x.p.Id).ToList();
            var files = await _uploadRepo.Query()
                .Where(f => f.ProgressId.HasValue && progressIds.Contains(f.ProgressId.Value))
                .ToListAsync();

            var dtos = data.Select(x => {
                var dto = _mapper.Map<ProgressDto>(x.p);
                dto.UserFullName = x.FullName ?? "—";
                dto.UserEmployeeCode = x.EmployeeCode ?? "—";
                dto.TaskTitle = x.TaskTitle ?? "—";
                dto.Files = files
                    .Where(f => f.ProgressId == dto.Id)
                    .Select(f => new UploadFileDto {
                        Id = f.Id,
                        FileName = f.FileName,
                        FilePath = f.FilePath,
                        CreatedAt = f.CreatedAt,
                        ProgressId = f.ProgressId
                    }).ToList();
                return dto;
            }).ToList();

            return new { total, data = dtos };
        }
    }
}

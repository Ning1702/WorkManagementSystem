using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Repositories;
using TaskStatusEnum = WorkManagementSystem.Domain.Enums.TaskStatus;
using TaskItem = WorkManagementSystem.Domain.Entities.TaskItem;

namespace WorkManagementSystem.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly IGenericRepository<TaskItem> _taskRepo;
        private readonly IGenericRepository<TaskAssignee> _assigneeRepo;
        private readonly IGenericRepository<UserUnit> _userUnitRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<TaskHistory> _historyRepo;  // ✅ MỚI
        private readonly INotificationService _notificationService; // ✅ MỚI
        private readonly IMapper _mapper;

        public TaskService(
            IGenericRepository<TaskItem> taskRepo,
            IGenericRepository<TaskAssignee> assigneeRepo,
            IGenericRepository<UserUnit> userUnitRepo,
            IGenericRepository<User> userRepo,
            IGenericRepository<TaskHistory> historyRepo,  // ✅ MỚI
            INotificationService notificationService, // ✅ MỚI
            IMapper mapper)
        {
            _taskRepo = taskRepo;
            _assigneeRepo = assigneeRepo;
            _userUnitRepo = userUnitRepo;
            _userRepo = userRepo;
            _historyRepo = historyRepo;  // ✅ MỚI
            _notificationService = notificationService; // ✅ MỚI
            _mapper = mapper;
        }

        public async Task<Guid?> GetManagerUnitId(Guid managerId)
        {
            var manager = await _userRepo.GetByIdAsync(managerId);
            return manager?.UnitId;
        }

        public async Task<TaskDto> Create(CreateTaskDto dto, Guid userId)
        {
            var task = _mapper.Map<TaskItem>(dto);
            task.Id = Guid.NewGuid();
            task.CreatedBy = userId;
            task.CreatedAt = DateTime.UtcNow;
            task.DueDate = dto.DueDate;
            await _taskRepo.AddAsync(task);

            if (dto.UserIds != null)
                foreach (var uid in dto.UserIds)
                    await _assigneeRepo.AddAsync(new TaskAssignee
                    { Id = Guid.NewGuid(), TaskId = task.Id, UserId = uid });

            if (dto.UnitIds != null)
                foreach (var unitId in dto.UnitIds)
                    await _assigneeRepo.AddAsync(new TaskAssignee
                    { Id = Guid.NewGuid(), TaskId = task.Id, UnitId = unitId });

            await _taskRepo.SaveAsync();
            return _mapper.Map<TaskDto>(task);
        }

        public async Task<object> Get(
            string keyword, int page, int size,
            string? status,
            Guid? userId = null,
            Guid? unitId = null)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 10 : size;

            var query = _taskRepo.Query();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.Title.Contains(keyword));

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<TaskStatusEnum>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            if (userId.HasValue)
            {
                var assignedTaskIds = _assigneeRepo.Query()
                    .Where(a => a.UserId == userId.Value)
                    .Select(a => a.TaskId);
                query = query.Where(x => assignedTaskIds.Contains(x.Id));
            }

            if (unitId.HasValue)
            {
                var unitTaskIds = _assigneeRepo.Query()
                    .Where(a => a.UnitId == unitId.Value)
                    .Select(a => a.TaskId);

                var userIdsInUnit = _userUnitRepo.Query()
                    .Where(uu => uu.UnitId == unitId.Value)
                    .Select(uu => uu.UserId);

                var userTaskIds = _assigneeRepo.Query()
                    .Where(a => a.UserId.HasValue && userIdsInUnit.Contains(a.UserId.Value))
                    .Select(a => a.TaskId);

                query = query.Where(x =>
                    unitTaskIds.Contains(x.Id) ||
                    userTaskIds.Contains(x.Id));
            }

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var taskIds = data.Select(x => x.Id).ToList();
            var assignees = await _assigneeRepo.Query()
                .Where(a => taskIds.Contains(a.TaskId) && a.UserId.HasValue)
                .Join(_userRepo.Query(),
                    a => a.UserId,
                    u => u.Id,
                    (a, u) => new { a.TaskId, u.Id, u.FullName, u.EmployeeCode })
                .ToListAsync();

            var taskDtos = _mapper.Map<List<TaskDto>>(data);
            foreach (var dto in taskDtos)
            {
                dto.Assignees = assignees
                    .Where(a => a.TaskId == dto.Id)
                    .Select(a => new TaskAssigneeDto 
                    { 
                        Id = (Guid)a.Id, 
                        FullName = a.FullName ?? "—", 
                        EmployeeCode = a.EmployeeCode ?? "—" 
                    })
                    .ToList();
            }

            return new { total, page, size, data = taskDtos };
        }

        // ✅ SỬA: Thêm Audit Log khi cập nhật Task
        public async Task<TaskDto> Update(Guid id, CreateTaskDto dto, Guid changedBy)
        {
            var task = await _taskRepo.GetByIdAsync(id)
                ?? throw new Exception("Task not found");

            // ✅ MỚI: Ghi lại lịch sử thay đổi
            if (task.Title != dto.Title)
                await _historyRepo.AddAsync(new TaskHistory
                {
                    Id = Guid.NewGuid(),
                    TaskId = id,
                    ChangedBy = changedBy,
                    FieldName = "Title",
                    OldValue = task.Title,
                    NewValue = dto.Title
                });

            if (task.DueDate != dto.DueDate)
                await _historyRepo.AddAsync(new TaskHistory
                {
                    Id = Guid.NewGuid(),
                    TaskId = id,
                    ChangedBy = changedBy,
                    FieldName = "DueDate",
                    OldValue = task.DueDate?.ToString("dd/MM/yyyy") ?? "Không có",
                    NewValue = dto.DueDate?.ToString("dd/MM/yyyy") ?? "Không có"
                });

            if (task.Description != dto.Description)
                await _historyRepo.AddAsync(new TaskHistory
                {
                    Id = Guid.NewGuid(),
                    TaskId = id,
                    ChangedBy = changedBy,
                    FieldName = "Description",
                    OldValue = task.Description,
                    NewValue = dto.Description
                });

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.DueDate = dto.DueDate;
            _taskRepo.Update(task);
            await _taskRepo.SaveAsync();
            return _mapper.Map<TaskDto>(task);
        }

        // ✅ SỬA: Soft delete thay vì xóa cứng
        public async Task Delete(Guid id)
        {
            var task = await _taskRepo.GetByIdAsync(id)
                ?? throw new Exception("Task not found");

            task.IsDeleted = true;  // ✅ Soft delete
            _taskRepo.Update(task);
            await _taskRepo.SaveAsync();
        }

        public async Task RemindTask(Guid taskId, Guid reminderId)
        {
            var task = await _taskRepo.GetByIdAsync(taskId)
                ?? throw new Exception("Task not found");

            var assignedUserIds = await _assigneeRepo.Query()
                .Where(a => a.TaskId == taskId && a.UserId.HasValue)
                .Select(a => a.UserId.Value)
                .ToListAsync();

            if (!assignedUserIds.Any())
                throw new Exception("Không có nhân viên nào được giao công việc này.");

            var reminderUser = await _userRepo.GetByIdAsync(reminderId);
            var reminderName = reminderUser?.FullName ?? "Quản lý";

            foreach (var userId in assignedUserIds)
            {
                await _notificationService.AddNotification(userId, $"Quản lý {reminderName} đã đôn đốc bạn về công việc: {task.Title}. Hãy khẩn trương hoàn thành!");
            }

            await _historyRepo.AddAsync(new TaskHistory
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ChangedBy = reminderId,
                FieldName = "Remind",
                OldValue = "N/A",
                NewValue = "Đã gửi nhắc nhở đôn đốc tiến độ"
            });
            await _historyRepo.SaveAsync();
        }
    }
}

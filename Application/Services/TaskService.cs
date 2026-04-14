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
        private readonly IMapper _mapper;

        public TaskService(
            IGenericRepository<TaskItem> taskRepo,
            IGenericRepository<TaskAssignee> assigneeRepo,
            IGenericRepository<UserUnit> userUnitRepo,
            IGenericRepository<User> userRepo,
            IMapper mapper)
        {
            _taskRepo = taskRepo;
            _assigneeRepo = assigneeRepo;
            _userUnitRepo = userUnitRepo;
            _userRepo = userRepo;
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
            task.DueDate = dto.DueDate;  // ✅ lưu deadline
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

            return new { total, page, size, data = _mapper.Map<List<TaskDto>>(data) };
        }

        public async Task<TaskDto> Update(Guid id, CreateTaskDto dto)
        {
            var task = await _taskRepo.GetByIdAsync(id)
                ?? throw new Exception("Task not found");
            task.Title = dto.Title;
            task.Description = dto.Description;
            task.DueDate = dto.DueDate;  // ✅ cập nhật deadline
            _taskRepo.Update(task);
            await _taskRepo.SaveAsync();
            return _mapper.Map<TaskDto>(task);
        }

        public async Task Delete(Guid id)
        {
            var task = await _taskRepo.GetByIdAsync(id)
                ?? throw new Exception("Task not found");
            _taskRepo.Delete(task);
            await _taskRepo.SaveAsync();
        }
    }
}
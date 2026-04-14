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
        private readonly IGenericRepository<TaskItem> _taskRepo;  // ✅ thêm
        private readonly IMapper _mapper;

        public ProgressService(
            IGenericRepository<Progress> repo,
            IGenericRepository<TaskItem> taskRepo,  // ✅ thêm
            IMapper mapper)
        {
            _repo = repo;
            _taskRepo = taskRepo;  // ✅ thêm
            _mapper = mapper;
        }

        public async Task<ProgressDto> Update(CreateProgressDto dto)
        {
            var progress = _mapper.Map<Progress>(dto);
            progress.Id = Guid.NewGuid();
            progress.Status = TaskStatus.Submitted;
            progress.UpdatedAt = DateTime.UtcNow;
            await _repo.AddAsync(progress);

            // ✅ Cập nhật status của Task → Submitted
            var task = await _taskRepo.GetByIdAsync(dto.TaskId);
            if (task != null)
            {
                task.Status = TaskStatus.Submitted;
                _taskRepo.Update(task);
            }

            await _repo.SaveAsync();
            return _mapper.Map<ProgressDto>(progress);
        }

        public async Task<object> GetAll(int page, int size, Guid? userId = null)
        {
            var query = _repo.Query();
            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);
            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(p => p.UpdatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
            return new { total, data = _mapper.Map<List<ProgressDto>>(data) };
        }
    }
}
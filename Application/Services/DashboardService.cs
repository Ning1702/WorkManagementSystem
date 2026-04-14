using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Infrastructure.Data;
using TaskStatus = WorkManagementSystem.Domain.Enums.TaskStatus;

namespace WorkManagementSystem.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDto> GetDashboard()
        {
            var tasks = await _context.Tasks.ToListAsync();
            var progresses = await _context.Progresses.ToListAsync();
            var units = await _context.Units.ToListAsync();

            return new DashboardDto
            {
                TotalTasks = tasks.Count,
                TotalUsers = await _context.Users.CountAsync(),
                TotalUnits = units.Count,
                TaskPending = progresses.Count(p => p.Status == TaskStatus.NotStarted),
                TaskInProgress = progresses.Count(p => p.Status == TaskStatus.InProgress),
                TaskApproved = progresses.Count(p => p.Status == TaskStatus.Approved),
                TaskRejected = progresses.Count(p => p.Status == TaskStatus.Rejected),
                ReportSubmitted = progresses.Count(p => p.Status == TaskStatus.Submitted),
                UnitSummaries = units.Select(u => new UnitSummaryDto
                {
                    UnitName = u.Name,
                    TotalTasks = _context.TaskAssignees
                        .Count(ta => ta.UnitId == u.Id),
                    ApprovedTasks = _context.TaskAssignees
                        .Count(ta => ta.UnitId == u.Id &&
                            _context.Progresses.Any(p => p.TaskId == ta.TaskId &&
                                p.Status == TaskStatus.Approved))
                }).ToList()
            };
        }

        public async Task<ManagerDashboardDto> GetManagerDashboard(Guid userId)
        {
            // Lấy phòng ban của Manager
            var userUnit = await _context.UserUnits
                .FirstOrDefaultAsync(uu => uu.UserId == userId);

            if (userUnit == null)
                return new ManagerDashboardDto { UnitName = "Chưa có phòng ban" };

            var unitId = userUnit.UnitId;
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == unitId);

            // Lấy tất cả thành viên trong phòng
            var memberIds = await _context.UserUnits
                .Where(uu => uu.UnitId == unitId)
                .Select(uu => uu.UserId)
                .ToListAsync();

            var members = await _context.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();

            // Lấy tất cả task của phòng
            var taskIds = await _context.TaskAssignees
                .Where(ta => ta.UnitId == unitId)
                .Select(ta => ta.TaskId)
                .Distinct()
                .ToListAsync();

            // Lấy tiến độ của các thành viên trong phòng
            var progresses = await _context.Progresses
                .Where(p => memberIds.Contains(p.UserId))
                .ToListAsync();

            // Thống kê từng thành viên
            var memberProgresses = members.Select(m =>
            {
                var myProgresses = progresses.Where(p => p.UserId == m.Id).ToList();
                return new MemberProgressDto
                {
                    FullName = m.FullName,
                    TotalTasks = myProgresses.Count,
                    ApprovedTasks = myProgresses.Count(p => p.Status == TaskStatus.Approved),
                    SubmittedTasks = myProgresses.Count(p => p.Status == TaskStatus.Submitted)
                };
            }).ToList();

            return new ManagerDashboardDto
            {
                UnitName = unit?.Name ?? "",
                TotalMembers = members.Count,
                TotalTasks = taskIds.Count,
                TaskPending = progresses.Count(p => p.Status == TaskStatus.NotStarted),
                TaskInProgress = progresses.Count(p => p.Status == TaskStatus.InProgress),
                TaskApproved = progresses.Count(p => p.Status == TaskStatus.Approved),
                TaskRejected = progresses.Count(p => p.Status == TaskStatus.Rejected),
                ReportSubmitted = progresses.Count(p => p.Status == TaskStatus.Submitted),
                MemberProgresses = memberProgresses
            };
        }
    }
}
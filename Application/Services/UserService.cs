using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Repositories;

namespace WorkManagementSystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IGenericRepository<User> _repo;
        private readonly IGenericRepository<UserUnit> _userUnitRepo;
        private readonly IMapper _mapper;

        public UserService(
            IGenericRepository<User> repo,
            IGenericRepository<UserUnit> userUnitRepo,
            IMapper mapper)
        {
            _repo = repo;
            _userUnitRepo = userUnitRepo;
            _mapper = mapper;
        }

        public async Task<List<UserDto>> GetAll()
            => _mapper.Map<List<UserDto>>(await _repo.Query().ToListAsync());

        public async Task<List<UserDto>> GetByManager(Guid managerId)
        {
            var manager = await _repo.GetByIdAsync(managerId);
            if (manager?.UnitId == null) return new List<UserDto>();

            var userIdsInUnit = await _userUnitRepo.Query()
                .Where(uu => uu.UnitId == manager.UnitId)
                .Select(uu => uu.UserId)
                .ToListAsync();

            var users = await _repo.Query()
                .Where(u => userIdsInUnit.Contains(u.Id) && u.Role != "Admin")
                .ToListAsync();

            return _mapper.Map<List<UserDto>>(users);
        }

        // ✅ Tìm kiếm nhân viên theo tên/mã NV/role/phòng
        public async Task<List<UserDto>> Search(string keyword, string? role, Guid? unitId)
        {
            var query = _repo.Query().Where(u => u.Role != "Admin");

            // Tìm theo tên hoặc mã NV hoặc username
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.Contains(keyword)) ||
                    (u.EmployeeCode != null && u.EmployeeCode.Contains(keyword)) ||
                    u.Username.Contains(keyword));

            // Filter theo role
            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            // Filter theo phòng ban
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
            _repo.Delete(user);
            await _repo.SaveAsync();
        }
    }
}
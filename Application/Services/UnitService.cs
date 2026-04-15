using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WorkManagementSystem.Application.DTOs;
using WorkManagementSystem.Application.Interfaces;
using WorkManagementSystem.Domain.Entities;
using WorkManagementSystem.Infrastructure.Repositories;

namespace WorkManagementSystem.Application.Services
{
    public class UnitService : IUnitService
    {
        private readonly IGenericRepository<Unit> _repo;
        private readonly IGenericRepository<UserUnit> _userUnitRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IMapper _mapper;

        public UnitService(
            IGenericRepository<Unit> repo,
            IGenericRepository<UserUnit> userUnitRepo,
            IGenericRepository<User> userRepo,
            IMapper mapper)
        {
            _repo = repo;
            _userUnitRepo = userUnitRepo;
            _userRepo = userRepo;
            _mapper = mapper;
        }

        public async Task<List<UnitDto>> GetAll()
            => _mapper.Map<List<UnitDto>>(await _repo.Query().ToListAsync());

        public async Task<UnitDto?> GetMyUnit(Guid userId)
        {
            var userUnit = await _userUnitRepo.Query()
                .Include(x => x.Unit)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (userUnit != null)
                return _mapper.Map<UnitDto>(userUnit.Unit);

            var user = await _userRepo.GetByIdAsync(userId);
            if (user?.UnitId != null)
            {
                var unit = await _repo.GetByIdAsync(user.UnitId.Value);
                return _mapper.Map<UnitDto>(unit);
            }

            return null;
        }

        public async Task<List<UserDto>> GetUsers(Guid unitId)
        {
            // Lấy ID từ bảng liên kết
            var userIdsFromMapping = await _userUnitRepo.Query()
                .Where(x => x.UnitId == unitId)
                .Select(x => x.UserId)
                .ToListAsync();

            // Lấy user từ cả 2 nguồn: UnitId trực tiếp HOẶC nằm trong danh sách Mapping
            var users = await _userRepo.Query()
                .Where(u => (u.UnitId == unitId || userIdsFromMapping.Contains(u.Id)) 
                            && u.IsApproved 
                            && !u.IsDeleted)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName ?? "—",
                    EmployeeCode = u.EmployeeCode ?? "—",
                    Role = u.Role,
                    UnitId = u.UnitId,
                    IsApproved = u.IsApproved
                })
                .ToListAsync();

            return users;
        }

        public async Task<UnitDto> Create(CreateUnitDto dto)
        {
            var unit = new Unit { Id = Guid.NewGuid(), Name = dto.Name };
            await _repo.AddAsync(unit);
            await _repo.SaveAsync();
            return _mapper.Map<UnitDto>(unit);
        }

        public async Task<UnitDto> Update(Guid id, CreateUnitDto dto)
        {
            var unit = await _repo.GetByIdAsync(id)
                ?? throw new Exception("Unit not found");
            unit.Name = dto.Name;
            _repo.Update(unit);
            await _repo.SaveAsync();
            return _mapper.Map<UnitDto>(unit);
        }

        // ✅ SỬA: Soft delete thay vì xóa cứng
        public async Task Delete(Guid id)
        {
            var unit = await _repo.GetByIdAsync(id)
                ?? throw new Exception("Unit not found");

            unit.IsDeleted = true;  // ✅ Soft delete
            _repo.Update(unit);
            await _repo.SaveAsync();
        }

        public async Task AddMember(Guid unitId, Guid userId)
        {
            var exists = await _userUnitRepo.Query()
                .AnyAsync(x => x.UnitId == unitId && x.UserId == userId);
            if (exists) throw new Exception("Thành viên đã thuộc đơn vị này!");

            await _userUnitRepo.AddAsync(new UserUnit
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                UserId = userId
            });
            await _userUnitRepo.SaveAsync();
        }

        public async Task RemoveMember(Guid unitId, Guid userId)
        {
            var userUnit = await _userUnitRepo.Query()
                .FirstOrDefaultAsync(x => x.UnitId == unitId && x.UserId == userId)
                ?? throw new Exception("Không tìm thấy thành viên!");

            _userUnitRepo.Delete(userUnit);
            await _userUnitRepo.SaveAsync();
        }
    }
}

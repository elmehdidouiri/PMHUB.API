using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Application.Mappings.EntityDto;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;

namespace PMHUB.Application.Services
{
    public class InternService : IInternService
    {
        private readonly IRepository<Intern> _internRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<InternAllocation> _internAllocationRepository;
        private readonly ILogger<InternService> _logger;

        public InternService(
            IRepository<Intern> internRepository,
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            IRepository<InternAllocation> internAllocationRepository,
            ILogger<InternService> logger)
        {
            _internRepository = internRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _internAllocationRepository = internAllocationRepository;
            _logger = logger;
        }

        public async Task<InternDto> CreateAsync(CreateInternDto dto, Guid currentUserId)
        {
            var supervisor = await GetNormalUserAsync(dto.SupervisorId);
            var role = await GetRoleAsync(dto.RoleId);
            await EnsureCanManageInternAsync(currentUserId, dto.SupervisorId);

            var existing = await _internRepository.FindAsync(i =>
                i.Name == dto.Name && i.SupervisorId == dto.SupervisorId);

            if (existing.Any())
                throw new ConflictException("Intern", dto.Name);

            var intern = new Intern
            {
                Name = dto.Name,
                RoleId = dto.RoleId,
                SupervisorId = dto.SupervisorId,
                CreatedAt = DateTime.UtcNow
            };

            await _internRepository.AddAsync(intern);
            await _internRepository.SaveChangesAsync();

            _logger.LogInformation("Intern {InternId} crÃ©Ã© avec succÃ¨s", intern.Id);
            return InternEntityDtoMapper.ToDto(intern, supervisor, role);
        }

        public async Task<IEnumerable<InternDto>> GetAllAsync()
        {
            var interns = (await _internRepository.GetAllAsync()).ToList();
            var supervisors = await LoadSupervisorsAsync(interns.Select(i => i.SupervisorId).Distinct());
            var roles = await LoadRolesAsync(interns.Select(i => i.RoleId).Distinct());

            return interns
                .OrderBy(i => i.Name)
                .Select(i => InternEntityDtoMapper.ToDto(i, supervisors.GetValueOrDefault(i.SupervisorId), roles.GetValueOrDefault(i.RoleId)))
                .ToList();
        }

        public async Task<InternDto> GetByIdAsync(Guid id)
        {
            var intern = await _internRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Intern", id);

            var supervisor = await GetNormalUserAsync(intern.SupervisorId);
            var role = await GetRoleAsync(intern.RoleId);
            return InternEntityDtoMapper.ToDto(intern, supervisor, role);
        }

        public async Task<InternDto> UpdateAsync(UpdateInternDto dto, Guid currentUserId)
        {
            var intern = await _internRepository.GetByIdAsync(dto.Id)
                ?? throw new NotFoundException("Intern", dto.Id);

            await EnsureCanManageInternAsync(currentUserId, intern.SupervisorId, dto.SupervisorId);
            var supervisor = await GetNormalUserAsync(dto.SupervisorId);
            var role = await GetRoleAsync(dto.RoleId);

            var existing = await _internRepository.FindAsync(i =>
                i.Id != dto.Id &&
                i.Name == dto.Name &&
                i.SupervisorId == dto.SupervisorId);

            if (existing.Any())
                throw new ConflictException("Intern", dto.Name);

            intern.Name = dto.Name;
            intern.RoleId = dto.RoleId;
            intern.SupervisorId = dto.SupervisorId;
            intern.UpdatedAt = DateTime.UtcNow;

            _internRepository.Update(intern);
            await _internRepository.SaveChangesAsync();

            return InternEntityDtoMapper.ToDto(intern, supervisor, role);
        }

        public async Task DeleteAsync(Guid id, Guid currentUserId)
        {
            var intern = await _internRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Intern", id);

            await EnsureCanManageInternAsync(currentUserId, intern.SupervisorId);

            var allocations = await _internAllocationRepository.FindAsync(a => a.InternId == id);
            if (allocations.Any())
            throw new BadRequestException("This intern cannot be deleted because they are already allocated to one or more projects.");

            _internRepository.Remove(intern);
            await _internRepository.SaveChangesAsync();
        }

        private async Task<NormalUser> GetNormalUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            return user as NormalUser
            ?? throw new BadRequestException("The selected supervisor must be a normal user.");
        }

        private async Task<Role> GetRoleAsync(Guid roleId)
        {
            return await _roleRepository.GetByIdAsync(roleId)
                ?? throw new NotFoundException("Role", roleId);
        }

        private async Task EnsureCanManageInternAsync(Guid currentUserId, params Guid[] supervisorIds)
        {
            var user = await _userRepository.GetByIdAsync(currentUserId)
                ?? throw new NotFoundException("User", currentUserId);

            if (user is Admin)
                return;

            if (user is not NormalUser)
            throw new ForbiddenException("You are not authorized to perform this action.");

            if (!supervisorIds.Contains(currentUserId))
            throw new ForbiddenException("Only an administrator or the assigned supervisor can manage this intern.");
        }

        private async Task<Dictionary<Guid, NormalUser>> LoadSupervisorsAsync(IEnumerable<Guid> supervisorIds)
        {
            var ids = supervisorIds.Distinct().ToList();
            var supervisors = await _userRepository.FindAsync(u => ids.Contains(u.Id));

            return supervisors
                .OfType<NormalUser>()
                .ToDictionary(u => u.Id, u => u);
        }

        private async Task<Dictionary<Guid, Role>> LoadRolesAsync(IEnumerable<Guid> roleIds)
        {
            var ids = roleIds.Distinct().ToList();
            var roles = await _roleRepository.FindAsync(r => ids.Contains(r.Id));
            return roles.ToDictionary(r => r.Id, r => r);
        }

    }
}

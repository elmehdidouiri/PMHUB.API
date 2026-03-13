using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMHUB.Application.Services.Implementation
{
    public class RoleService : IRoleService
    {
        private readonly IRepository<Role> _repository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IRepository<Role> repository, ILogger<RoleService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // ── CREATE
        public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
        {
            _logger.LogInformation("Création d'un rôle : {RoleName}", dto.Name);

            var existing = await _repository.FindAsync(r => r.Name == dto.Name);
            if (existing.Any())
            {
                _logger.LogWarning("Le rôle {RoleName} existe déjà", dto.Name);
                throw new ConflictException("Role", dto.Name);
            }

            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(role);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Rôle {RoleId} créé avec succès", role.Id);

            return MapToDto(role);
        }

        // ── GET ALL
        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            _logger.LogInformation("Récupération de tous les rôles");
            var roles = await _repository.GetAllAsync();
            return roles.Select(MapToDto);
        }

        // ── GET BY ID
        public async Task<RoleDto?> GetRoleByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération du rôle {RoleId}", id);

            var role = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Role", id);

            return MapToDto(role);
        }

        // ── UPDATE
        public async Task<RoleDto?> UpdateRoleAsync(Guid id, UpdateRoleDto dto)
        {
            _logger.LogInformation("Mise à jour du rôle {RoleId}", id);

            var role = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Role", id);

            var existing = await _repository.FindAsync(r => r.Name == dto.Name && r.Id != id);
            if (existing.Any())
            {
                _logger.LogWarning("Le rôle {RoleName} existe déjà", dto.Name);
                throw new ConflictException("Role", dto.Name);
            }

            role.Name = dto.Name;
            role.Description = dto.Description;
            role.IsActive = dto.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            _repository.Update(role);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Rôle {RoleId} mis à jour avec succès", id);

            return MapToDto(role);
        }

        // ── DELETE
        public async Task<bool> DeleteRoleAsync(Guid id)
        {
            _logger.LogInformation("Suppression du rôle {RoleId}", id);

            var role = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Role", id);

            _repository.Remove(role);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Rôle {RoleId} supprimé avec succès", id);

            return true;
        }

        private static RoleDto MapToDto(Role r) => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
        };
    }
}
using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;

namespace PMHUB.Application.Services.Implementation
{
    public class AdminService : IAdminService
    {
        private readonly IRepository<Admin> _repo;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IRepository<Admin> repo, ILogger<AdminService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

         public async Task<IEnumerable<AdminDto>> GetAllAdminsAsync()
        {
            _logger.LogInformation("Récupération de tous les admins.");

            try
            {
                var admins = await _repo.GetAllAsync();
                return admins.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des admins.");
                throw;
            }
        }

         public async Task<AdminDto?> GetAdminByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération de l'admin {AdminId}.", id);

            try
            {
                var admin = await _repo.GetByIdAsync(id)
                    ?? throw new NotFoundException("Admin", id);

                return MapToDto(admin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'admin {AdminId}.", id);
                throw;
            }
        }

         public async Task<AdminDto> CreateAdminAsync(CreateAdminDto dto)
        {
            _logger.LogInformation("Création d'un nouvel admin {Email}.", dto.Email);

            try
            {
                var existing = await _repo.FindAsync(a => a.Email == dto.Email);
                if (existing.Any())
                    throw new ConflictException("Admin", dto.Email);

                var admin = new Admin
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddAsync(admin);
                await _repo.SaveChangesAsync();

                _logger.LogInformation("Admin créé avec succès {AdminId}.", admin.Id);
                return MapToDto(admin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'admin {Email}.", dto.Email);
                throw;
            }
        }

         public async Task<AdminDto> UpdateAdminAsync(Guid id, UpdateAdminDto dto)
        {
            _logger.LogInformation("Mise à jour de l'admin {AdminId}.", id);

            try
            {
                var admin = await _repo.GetByIdAsync(id)
                    ?? throw new NotFoundException("Admin", id);

                if (!string.IsNullOrWhiteSpace(dto.FirstName))
                    admin.FirstName = dto.FirstName;

                if (!string.IsNullOrWhiteSpace(dto.LastName))
                    admin.LastName = dto.LastName;

                if (!string.IsNullOrWhiteSpace(dto.Email))
                    admin.Email = dto.Email;

                admin.UpdatedAt = DateTime.UtcNow;

                _repo.Update(admin);
                await _repo.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} mis à jour avec succès.", id);
                return MapToDto(admin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'admin {AdminId}.", id);
                throw;
            }
        }

         public async Task<bool> DeleteAdminAsync(Guid id)
        {
            _logger.LogInformation("Suppression de l'admin {AdminId}.", id);

            try
            {
                var admin = await _repo.GetByIdAsync(id)
                    ?? throw new NotFoundException("Admin", id);

                _repo.Remove(admin);
                await _repo.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} supprimé avec succès.", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'admin {AdminId}.", id);
                throw;
            }
        }

         private static AdminDto MapToDto(Admin admin) => new()
        {
            Id = admin.Id,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            Email = admin.Email,
            CreatedAt = admin.CreatedAt,
            UpdatedAt = admin.UpdatedAt
        };
    }
}
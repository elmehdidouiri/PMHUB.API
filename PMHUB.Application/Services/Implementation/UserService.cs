using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Application.Mappings.EntityDto;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IRepository<Role> roleRepository,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _logger = logger;
        }

        // ── CREATE 
        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            _logger.LogInformation("Création d'un utilisateur avec l'email {Email}", dto.Email);

            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("L'utilisateur {Email} existe déjà", dto.Email);
                throw new ConflictException("User", dto.Email);
            }

            await (_roleRepository.GetByIdAsync(dto.RoleId.Value)
                ?? throw new NotFoundException("Role", dto.RoleId.Value));

             var user = new NormalUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = dto.RoleId.Value,
                IsApproved = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Utilisateur {UserId} créé avec succès", user.Id);
            return UserEntityDtoMapper.ToDto(user);
        }

        // ── GET BY ID 
        public async Task<UserDto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération de l'utilisateur {UserId}", id);

            var user = await _userRepository.GetByIdWithRoleAsync(id)
                ?? throw new NotFoundException("User", id);

            return UserEntityDtoMapper.ToDto(user);
        }

        // ── GET BY EMAIL  
        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            throw new BadRequestException("Email cannot be empty.");

            _logger.LogInformation("Récupération de l'utilisateur avec l'email {Email}", email);

            var user = await _userRepository.GetByEmailAsync(email)
                ?? throw new NotFoundException("User", email);

            return UserEntityDtoMapper.ToDto(user);
        }

        // ── GET ALL 
        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            _logger.LogInformation("Récupération de tous les utilisateurs");
            var users = await _userRepository.GetAllWithRoleAsync();
            return users.Select(UserEntityDtoMapper.ToDto);
        }

        public async Task<IEnumerable<UserDto>> GetByRoleIdAsync(Guid roleId)
        {
            _logger.LogInformation("Récupération des utilisateurs pour le rôle {RoleId}", roleId);

            await (_roleRepository.GetByIdAsync(roleId)
                ?? throw new NotFoundException("Role", roleId));

            var users = await _userRepository.GetByRoleIdAsync(roleId);
            return users.Select(UserEntityDtoMapper.ToDto);
        }

        // ── UPDATE 
        public async Task UpdateUserAsync(UpdateUserDto dto)
        {
            _logger.LogInformation("Mise à jour de l'utilisateur {UserId}", dto.Id);

             var user = await _userRepository.GetByIdAsync(dto.Id) as NormalUser
                ?? throw new NotFoundException("User", dto.Id);

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var emailExists = await _userRepository.GetByEmailAsync(dto.Email);
                if (emailExists != null)
                {
                    _logger.LogWarning("L'email {Email} est déjà utilisé", dto.Email);
                    throw new ConflictException("User", dto.Email);
                }
                user.Email = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName;

            if (dto.RoleId.HasValue)
            {
                await (_roleRepository.GetByIdAsync(dto.RoleId.Value)
                    ?? throw new NotFoundException("Role", dto.RoleId.Value));
                user.RoleId = dto.RoleId.Value;
            }

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Utilisateur {UserId} mis à jour avec succès", dto.Id);
        }

        // ── DELETE 
        public async Task DeleteUserAsync(Guid userId)
        {
            _logger.LogInformation("Suppression de l'utilisateur {UserId}", userId);

             var user = await _userRepository.GetByIdAsync(userId) as NormalUser
                ?? throw new NotFoundException("User", userId);

            if (user.ProjectMembers.Any())
            {
                _logger.LogWarning("Impossible de supprimer l'utilisateur {UserId} car assigné à des projets", userId);
                throw new BadRequestException(
                    "Impossible de supprimer cet utilisateur car il est assigné à des projets actifs.");
            }

            _userRepository.Remove(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Utilisateur {UserId} supprimé avec succès", userId);
        }

        // ── APPROVE 
        public async Task ApproveUserAsync(ApproveUserDto dto, Guid adminId)
        {
            _logger.LogInformation("Approbation de l'utilisateur {UserId} par l'admin {AdminId}",
                dto.UserId, adminId);

             var user = await _userRepository.GetByIdAsync(dto.UserId) as NormalUser
                ?? throw new NotFoundException("User", dto.UserId);

             var admin = await _userRepository.GetByIdAsync(adminId) as Admin
                ?? throw new NotFoundException("Admin", adminId);

            if (user.IsApproved && dto.IsApproved)
            throw new BadRequestException("This user has already been approved.");

            user.IsApproved = dto.IsApproved;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedById = adminId;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Utilisateur {UserId} approuvé avec succès", dto.UserId);
        }

        // ── VALIDATE LOGIN 
        public async Task<bool> ValidateLoginAsync(string email, string password)
        {
            _logger.LogInformation("Validation de login pour l'email {Email}", email);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new BadRequestException("Email and password are required.");

            var user = await _userRepository.GetByEmailAsync(email)
                ?? throw new NotFoundException("User", email);

             if (user is NormalUser normalUser)
            {
                if (!normalUser.IsActive)
            throw new ForbiddenException("This account is deactivated.");

                if (!normalUser.IsApproved)
            throw new ForbiddenException("This account has not been approved yet.");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new BadRequestException("Invalid email or password.");

            _logger.LogInformation("Login validé pour l'utilisateur {UserId}", user.Id);
            return true;
        }

    }
}

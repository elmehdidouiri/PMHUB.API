using PMHUB.Application.Exceptions;
using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            // Vérifier doublon email
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new ConflictException("User", dto.Email);

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                IsApproved = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<UserDto> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("User", id);

            return MapToDto(user);
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new BadRequestException("L'email ne peut pas être vide.");

            var user = await _userRepository.GetByEmailAsync(email)
                ?? throw new NotFoundException("User", email);

            return MapToDto(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task UpdateUserAsync(UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.Id)
                ?? throw new NotFoundException("User", dto.Id);

            // Vérifier doublon email si changé
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var emailExists = await _userRepository.GetByEmailAsync(dto.Email);
                if (emailExists != null)
                    throw new ConflictException("User", dto.Email);

                user.Email = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName;

            if (dto.Role.HasValue)
                user.Role = dto.Role.Value;

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            // Vérifier si l'utilisateur a des projets actifs
            if (user.ProjectMembers.Any())
                throw new BadRequestException(
                    "Impossible de supprimer cet utilisateur car il est assigné à des projets actifs.");

            _userRepository.Remove(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task ApproveUserAsync(ApproveUserDto dto, Guid adminId)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId)
                ?? throw new NotFoundException("User", dto.UserId);

            // Vérifier que l'admin existe
            var admin = await _userRepository.GetByIdAsync(adminId)
                ?? throw new NotFoundException("Admin", adminId);

            if (user.IsApproved && dto.IsApproved)
                throw new BadRequestException("Cet utilisateur est déjà approuvé.");

            user.IsApproved = dto.IsApproved;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedById = adminId;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> ValidateLoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new BadRequestException("Email et mot de passe sont obligatoires.");

            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
                throw new NotFoundException("User", email);

            if (!user.IsActive)
                throw new ForbiddenException("Ce compte est désactivé.");

            if (!user.IsApproved)
                throw new ForbiddenException("Ce compte n'est pas encore approuvé.");

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new BadRequestException("Email ou mot de passe incorrect.");

            return true;
        }

        private static UserDto MapToDto(User user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            IsApproved = user.IsApproved,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Interfaces;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PMHUB.Application.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        private readonly string _jwtSecret;
        private readonly int _jwtExpirationMinutes;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AuthService(IUserRepository userRepository,IRepository<Role> roleRepository,IEmailService emailService,IConfiguration configuration,ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _emailService = emailService;
            _logger = logger;

            _jwtSecret = configuration["JwtSettings:Secret"]
                ?? throw new ArgumentNullException("JwtSettings:Secret");
            _jwtExpirationMinutes = int.Parse(
                configuration["JwtSettings:ExpirationMinutes"] ?? "60");
            _jwtIssuer = configuration["JwtSettings:Issuer"] ?? "PMHub";
            _jwtAudience = configuration["JwtSettings:Audience"] ?? "PMHubClients";
 
        }

        // ── REGISTER  
        public async Task RegisterAsync(RegisterDto dto)
        {
            _logger.LogInformation("Nouvelle tentative d'inscription pour {Email}", dto.Email);

             var existingUsers = await _userRepository.FindAsync(u => u.Email == dto.Email);
            if (existingUsers.Any())
                throw new ConflictException("User", dto.Email);

             var role = await _roleRepository.GetByIdAsync(dto.RoleId)
                ?? throw new NotFoundException("Role", dto.RoleId);

            var newUser = new NormalUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = dto.RoleId,
                IsApproved = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Utilisateur {UserId} créé avec succès.", newUser.Id);
        }


        // ── LOGIN 
        public async Task<AuthSuccessDto> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("Tentative de connexion pour {Email}", dto.Email);

            var user = await _userRepository.GetByEmailAsync(dto.Email)
                ?? throw new UnauthorizedException("Email ou mot de passe incorrect.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Mot de passe incorrect pour {Email}", dto.Email);
                throw new UnauthorizedException("Email ou mot de passe incorrect.");
            }

            if (user is NormalUser normalUser)
            {
                if (!normalUser.IsActive)
                    throw new ForbiddenException("Ce compte est désactivé.");

                if (!normalUser.IsApproved)
                    throw new ForbiddenException("Votre compte doit être approuvé par un admin.");

                _logger.LogInformation("NormalUser {UserId} connecté avec succès", user.Id);
                return GenerateToken(user, normalUser.Role?.Name ?? string.Empty, false);
            }
            if (user is Admin admin)
            {
                 admin.LastLoginAt = DateTime.UtcNow;
                admin.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(admin);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("Admin {UserId} connecté avec succès", user.Id);
                return GenerateToken(user, "Admin", true);
            }

            throw new UnauthorizedException("Type d'utilisateur non reconnu.");
        }

        // ── GET PENDING USERS  
        public async Task<IEnumerable<UserDto>> GetPendingUsersAsync()
        {
            _logger.LogInformation("Récupération des utilisateurs en attente d'approbation");

            var pendingUsers = await _userRepository.FindAsync(
                u => u is NormalUser && !((NormalUser)u).IsApproved);

            return pendingUsers.Select(u =>
            {
                var normalUser = u as NormalUser;
                return new UserDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    RoleId = normalUser?.RoleId ?? Guid.Empty,
                    RoleName = normalUser?.Role?.Name,
                    IsApproved = normalUser?.IsApproved ?? false,
                    IsActive = normalUser?.IsActive ?? false,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                };
            });
        }


        // ── APPROVE / DECLINE  
        public async Task ApproveUserAsync(ApproveUserDto dto, Guid adminId)
        {
            _logger.LogInformation("Approbation/Refus utilisateur {UserId}", dto.UserId);

            var user = await _userRepository.GetNormalUserByIdAsync(dto.UserId)
                ?? throw new NotFoundException("User", dto.UserId);

            if (user.IsApproved && dto.IsApproved)
                throw new BadRequestException("Cet utilisateur est déjà approuvé.");

            if (dto.IsApproved)
            {
                 user.IsApproved = true;
                user.ApprovedById = adminId;
                user.ApprovedAt = DateTime.UtcNow;
            }
            else
            {
                 user.IsApproved = false;
                user.ApprovedById = null;
                user.ApprovedAt = null;
            }

            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            if (dto.IsApproved)
            {
                _logger.LogInformation("Envoi email approbation à {Email}", user.Email);
                await _emailService.SendApprovalEmailAsync(user.Email, user.FirstName);
            }
            else
            {
                _logger.LogInformation("Envoi email rejet à {Email}", user.Email);
                await _emailService.SendRejectionEmailAsync(user.Email, user.FirstName);
            }

            _logger.LogInformation("Utilisateur {UserId} — IsApproved={IsApproved}",
                dto.UserId, dto.IsApproved);
        }
        // ── HELPER — Générer JWT  
        private AuthSuccessDto GenerateToken(User user, string roleName, bool isAdmin)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email,          user.Email),
                new Claim(ClaimTypes.Name,           $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role,           roleName),
                new Claim("isAdmin",                 isAdmin.ToString().ToLower())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new AuthSuccessDto
            {
                Token = tokenHandler.WriteToken(token),
                Expiration = token.ValidTo,
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleName = roleName,
                IsAdmin = isAdmin
            };
        }
    }
}
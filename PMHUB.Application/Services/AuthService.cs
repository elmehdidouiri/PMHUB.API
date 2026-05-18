using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Interfaces;
using PMHUB.Application.IServices;
using PMHUB.Application.Jwt;
using PMHUB.Application.Mappings.EntityDto;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PMHUB.Application.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<PasswordResetCode> _passwordResetCodeRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        private readonly string _jwtSecret;
        private readonly int _jwtExpirationMinutes;
        private readonly int _refreshTokenExpirationHours;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        private readonly AuthSettings _authSettings;

        public AuthService(
            IUserRepository userRepository,
            IRepository<Role> roleRepository,
            IRepository<PasswordResetCode> passwordResetCodeRepository,
            IEmailService emailService,
            IOptions<JwtSettings> jwtOptions,
            IOptions<AuthSettings> authOptions,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _passwordResetCodeRepository = passwordResetCodeRepository;
            _emailService = emailService;
            _logger = logger;
            _authSettings = authOptions.Value;

            var jwt = jwtOptions.Value;
            if (string.IsNullOrWhiteSpace(jwt.Secret))
            {
                throw new ArgumentNullException(nameof(jwtOptions), "JwtSettings:Secret is required.");
            }

            _jwtSecret = jwt.Secret;
            _jwtExpirationMinutes = jwt.ExpirationMinutes > 0 ? jwt.ExpirationMinutes : 30;
            _refreshTokenExpirationHours = jwt.RefreshTokenExpirationHours > 0 ? jwt.RefreshTokenExpirationHours : 24;
            _jwtIssuer = string.IsNullOrWhiteSpace(jwt.Issuer) ? "PMHub" : jwt.Issuer;
            _jwtAudience = string.IsNullOrWhiteSpace(jwt.Audience) ? "PMHubClients" : jwt.Audience;
        }

        public async Task RegisterAsync(RegisterDto dto)
        {
            _logger.LogInformation("Nouvelle tentative d'inscription pour {Email}", dto.Email);

            var existingUsers = await _userRepository.FindAsync(u => u.Email == dto.Email);
            if (existingUsers.Any())
                throw new ConflictException("User", dto.Email);

            var roleId = dto.RoleId ?? throw new BadRequestException("Role ID is required.");
            var role = await _roleRepository.GetByIdAsync(roleId)
                ?? throw new NotFoundException("Role", roleId);

            var newUser = new NormalUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = roleId,
                IsApproved = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Utilisateur {UserId} cree avec succes.", newUser.Id);
        }

        public async Task<AuthSuccessDto> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("Tentative de connexion pour {Email}", dto.Email);

            var user = await _userRepository.GetByEmailAsync(dto.Email)
                ?? throw new UnauthorizedException("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Mot de passe incorrect pour {Email}", dto.Email);
                throw new UnauthorizedException("Invalid email or password.");
            }

            if (user is NormalUser normalUser)
            {
                if (!normalUser.IsActive)
                    throw new ForbiddenException("This account is deactivated.");

                if (!normalUser.IsApproved)
                    throw new ForbiddenException("Your account must be approved by an administrator before you can sign in.");

                _logger.LogInformation("NormalUser {UserId} connecte avec succes", user.Id);
                return await CompleteLoginAsync(user, normalUser.Role?.Name ?? string.Empty, false, CancellationToken.None);
            }

            if (user is Admin admin)
            {
                admin.LastLoginAt = DateTime.UtcNow;
                admin.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(admin);

                _logger.LogInformation("Admin {UserId} connecte avec succes", user.Id);
                return await CompleteLoginAsync(user, "Admin", true, CancellationToken.None);
            }

            throw new UnauthorizedException("The authenticated user type is not supported.");
        }

        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto)
        {
            var normalizedEmail = dto.Email.Trim();
            _logger.LogInformation("Password reset code requested for {Email}", normalizedEmail);

            var user = await _userRepository.GetByEmailAsync(normalizedEmail)
                ?? throw new NotFoundException("This email does not exist. Please register first.");

            var now = DateTime.UtcNow;
            var activeCodes = await _passwordResetCodeRepository.FindAsync(code =>
                code.UserId == user.Id && code.ConsumedAtUtc == null && code.ExpiresAtUtc > now);

            foreach (var activeCode in activeCodes)
            {
                activeCode.ConsumedAtUtc = now;
                _passwordResetCodeRepository.Update(activeCode);
            }

            var codeValue = GenerateVerificationCode();
            var resetCode = new PasswordResetCode
            {
                UserId = user.Id,
                CodeHash = HashSecret(codeValue),
                ExpiresAtUtc = now.AddMinutes(_authSettings.PasswordResetCodeExpirationMinutes),
                CreatedAtUtc = now
            };

            await _passwordResetCodeRepository.AddAsync(resetCode);
            await _passwordResetCodeRepository.SaveChangesAsync();

            await _emailService.SendPasswordResetCodeAsync(
                user.Email,
                user.FirstName,
                codeValue,
                _authSettings.PasswordResetCodeExpirationMinutes);

            return new ForgotPasswordResponseDto
            {
                ExpiresInMinutes = _authSettings.PasswordResetCodeExpirationMinutes,
                Message = $"A verification code has been sent to your email. It expires in {_authSettings.PasswordResetCodeExpirationMinutes} minutes."
            };
        }

        public async Task<VerifyPasswordResetCodeResponseDto> VerifyPasswordResetCodeAsync(VerifyPasswordResetCodeDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email.Trim())
                ?? throw new NotFoundException("This email does not exist. Please register first.");

            var now = DateTime.UtcNow;
            var resetCode = (await _passwordResetCodeRepository.FindAsync(code =>
                    code.UserId == user.Id &&
                    code.ConsumedAtUtc == null &&
                    code.ExpiresAtUtc > now))
                .OrderByDescending(code => code.CreatedAtUtc)
                .FirstOrDefault()
                ?? throw new BadRequestException("The verification code is invalid or has expired. Please request a new code.");

            if (resetCode.Attempts >= _authSettings.MaxPasswordResetAttempts)
                throw new BadRequestException("Too many incorrect attempts. Please request a new verification code.");

            if (!string.Equals(resetCode.CodeHash, HashSecret(dto.Code.Trim()), StringComparison.Ordinal))
            {
                resetCode.Attempts++;
                _passwordResetCodeRepository.Update(resetCode);
                await _passwordResetCodeRepository.SaveChangesAsync();
                throw new BadRequestException("The verification code is incorrect.");
            }

            var resetToken = GenerateResetToken();
            resetCode.VerifiedAtUtc = now;
            resetCode.ResetTokenHash = HashSecret(resetToken);
            resetCode.ResetTokenExpiresAtUtc = now.AddMinutes(_authSettings.PasswordResetTokenExpirationMinutes);

            _passwordResetCodeRepository.Update(resetCode);
            await _passwordResetCodeRepository.SaveChangesAsync();

            return new VerifyPasswordResetCodeResponseDto
            {
                ResetToken = resetToken,
                ExpiresInMinutes = _authSettings.PasswordResetTokenExpirationMinutes,
                Message = $"Code verified. You can now set a new password within {_authSettings.PasswordResetTokenExpirationMinutes} minutes."
            };
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email.Trim())
                ?? throw new NotFoundException("This email does not exist. Please register first.");

            if (!string.Equals(dto.NewPassword, dto.ConfirmPassword, StringComparison.Ordinal))
                throw new BadRequestException("Password confirmation does not match.");

            var resetTokenHash = HashSecret(dto.ResetToken.Trim());
            var now = DateTime.UtcNow;
            var resetCode = (await _passwordResetCodeRepository.FindAsync(code =>
                    code.UserId == user.Id &&
                    code.ResetTokenHash == resetTokenHash &&
                    code.VerifiedAtUtc != null &&
                    code.ConsumedAtUtc == null &&
                    code.ResetTokenExpiresAtUtc != null &&
                    code.ResetTokenExpiresAtUtc > now))
                .OrderByDescending(code => code.VerifiedAtUtc)
                .FirstOrDefault()
                ?? throw new BadRequestException("The reset session is invalid or has expired. Please request a new code.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = now;
            _userRepository.Update(user);

            resetCode.ConsumedAtUtc = now;
            _passwordResetCodeRepository.Update(resetCode);

            await _passwordResetCodeRepository.SaveChangesAsync();
        }

        public async Task<AuthSuccessDto> RefreshTokensAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
        {
            var refreshPrincipal = ReadRefreshToken(dto.RefreshToken);
            var userIdValue = refreshPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdValue, out var userId))
                throw new UnauthorizedException("Invalid or expired refresh token.");

            var user = await _userRepository.GetByIdForAuthAsync(userId)
                ?? throw new UnauthorizedException("Invalid or expired refresh token.");

            if (user is NormalUser normalUser)
            {
                if (!normalUser.IsActive)
                    throw new ForbiddenException("This account is deactivated.");

                if (!normalUser.IsApproved)
                    throw new ForbiddenException("Your account must be approved by an administrator before you can sign in.");

                return await PersistNewRefreshSessionAsync(user, normalUser.Role?.Name ?? string.Empty, false, cancellationToken);
            }

            if (user is Admin)
            {
                return await PersistNewRefreshSessionAsync(user, "Admin", true, cancellationToken);
            }

            throw new UnauthorizedException("The authenticated user type is not supported.");
        }

        public async Task<IEnumerable<UserDto>> GetPendingUsersAsync()
        {
            _logger.LogInformation("Recuperation des utilisateurs en attente d'approbation");

            var pendingUsers = await _userRepository.GetPendingUsersAsync();

            return pendingUsers.Select(UserEntityDtoMapper.ToDto);
        }

        public async Task ApproveUserAsync(ApproveUserDto dto, Guid adminId)
        {
            _logger.LogInformation("Approbation/Refus utilisateur {UserId}", dto.UserId);

            var user = await _userRepository.GetNormalUserByIdAsync(dto.UserId)
                ?? throw new NotFoundException("User", dto.UserId);

            if (user.IsApproved && dto.IsApproved)
                throw new BadRequestException("This user has already been approved.");

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
                _logger.LogInformation("Envoi email approbation a {Email}", user.Email);
                await _emailService.SendApprovalEmailAsync(user.Email, user.FirstName);
            }
            else
            {
                _logger.LogInformation("Envoi email rejet a {Email}", user.Email);
                await _emailService.SendRejectionEmailAsync(user.Email, user.FirstName);
            }

            _logger.LogInformation("Utilisateur {UserId} - IsApproved={IsApproved}",
                dto.UserId, dto.IsApproved);
        }

        private async Task<AuthSuccessDto> CompleteLoginAsync(User user, string roleName, bool isAdmin, CancellationToken cancellationToken)
            => await PersistNewRefreshSessionAsync(user, roleName, isAdmin, cancellationToken);

        private Task<AuthSuccessDto> PersistNewRefreshSessionAsync(
            User user,
            string roleName,
            bool isAdmin,
            CancellationToken cancellationToken)
        {
            var accessToken = CreateAccessToken(user, roleName, isAdmin);
            var refreshToken = CreateRefreshToken(user, roleName, isAdmin);

            return Task.FromResult(new AuthSuccessDto
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleName = roleName,
                IsAdmin = isAdmin
            });
        }

        private string CreateAccessToken(User user, string roleName, bool isAdmin)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("isAdmin", isAdmin.ToString().ToLowerInvariant())
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
            return tokenHandler.WriteToken(token);
        }

        private string CreateRefreshToken(User user, string roleName, bool isAdmin)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("isAdmin", isAdmin.ToString().ToLowerInvariant()),
                new Claim("token_type", _authSettings.RefreshTokenType)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_refreshTokenExpirationHours),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string GenerateVerificationCode()
            => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        private static string GenerateResetToken()
            => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        private static string HashSecret(string secret)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
            return Convert.ToHexString(bytes);
        }

        private ClaimsPrincipal ReadRefreshToken(string refreshToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(
                    refreshToken,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = _jwtIssuer,
                        ValidAudience = _jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                        ClockSkew = TimeSpan.Zero
                    },
                    out var validatedToken);

                if (validatedToken is not JwtSecurityToken)
                    throw new UnauthorizedException("Invalid or expired refresh token.");

                var tokenType = principal.FindFirstValue("token_type");
                if (!string.Equals(tokenType, _authSettings.RefreshTokenType, StringComparison.Ordinal))
                    throw new UnauthorizedException("Invalid or expired refresh token.");

                return principal;
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
            {
                throw new UnauthorizedException("Invalid or expired refresh token.");
            }
        }
    }
}

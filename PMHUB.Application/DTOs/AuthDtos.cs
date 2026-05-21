using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthSuccessDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }

        /// <summary>JWT d'acces (courte duree). Serialise en <c>token</c> pour les clients existants.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Jeton de refresh chiffre, autonome et sans persistance en base.</summary>
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordResponseDto
    {
        public int ExpiresInMinutes { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class VerifyPasswordResetCodeDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must contain exactly 6 digits.")]
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyPasswordResetCodeResponseDto
    {
        public string ResetToken { get; set; } = string.Empty;
        public int ExpiresInMinutes { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reset token is required.")]
        public string ResetToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Password confirmation does not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

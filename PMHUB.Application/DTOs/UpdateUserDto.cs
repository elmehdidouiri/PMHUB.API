using System;
using System.ComponentModel.DataAnnotations;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.DTOs
{
    public class UpdateUserDto
    {
        public Guid Id { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        public string? Email { get; set; }

        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Password confirmation does not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

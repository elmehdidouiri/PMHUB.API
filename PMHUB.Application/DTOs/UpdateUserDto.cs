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
}
using System;

namespace PMHUB.Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public Guid RoleId { get; set; }
        public string? RoleName { get; set; }

         public bool IsAdmin { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }

         public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
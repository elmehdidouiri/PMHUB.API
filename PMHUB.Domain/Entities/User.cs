using PMHUB.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();


        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public Guid? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public User? ApprovedBy { get; set; }

        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }

        [Required]
        public RoleType Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

         public ICollection<HourEntry> HourEntries { get; set; } = new List<HourEntry>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class Intern
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public Guid SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        public User Supervisor { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<HourEntry> HourEntries { get; set; } = new List<HourEntry>();
    }
}
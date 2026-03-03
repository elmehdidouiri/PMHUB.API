using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Domain.Entities
{
    public class Sprint
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
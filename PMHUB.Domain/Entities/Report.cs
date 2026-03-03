using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class Report
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();


        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public Guid CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public User CreatedBy { get; set; } = null!;

        [Required]
        public string Data { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
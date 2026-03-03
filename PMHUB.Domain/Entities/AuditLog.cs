using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Action { get; set; } = string.Empty;

        public Guid PerformedById { get; set; }
        [ForeignKey("PerformedById")]
        public User PerformedBy { get; set; } = null!;

        public string? EntityName { get; set; }
        public int? EntityId { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
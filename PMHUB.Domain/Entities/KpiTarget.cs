using System;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Domain.Entities
{
    public class KpiTarget
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();


        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty; // "OTD", "Effectiveness", "CSAT"

        [Required]
        public float TargetValue { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class HourEntryInternSupervision
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid HourEntryId { get; set; }
        [ForeignKey(nameof(HourEntryId))]
        public HourEntry HourEntry { get; set; } = null!;

        [Required]
        public Guid InternAllocationId { get; set; }
        [ForeignKey(nameof(InternAllocationId))]
        public InternAllocation InternAllocation { get; set; } = null!;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Hours { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

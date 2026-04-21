using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class InternHourEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid InternAllocationId { get; set; }

        [ForeignKey(nameof(InternAllocationId))]
        public InternAllocation InternAllocation { get; set; } = null!;

        [Required]
        public Guid BookedByUserId { get; set; }

        [ForeignKey(nameof(BookedByUserId))]
        public NormalUser BookedByUser { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Hours { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

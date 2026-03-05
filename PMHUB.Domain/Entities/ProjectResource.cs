using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class ProjectResource
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; } = null!;

        [Required, MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerUnit { get; set; } = 0m;

        public int Quantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost => PricePerUnit * Quantity;

        [MaxLength(100)]
        public string? CostCenter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
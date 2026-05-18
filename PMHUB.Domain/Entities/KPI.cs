using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class KPI
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

         [Column(TypeName = "decimal(18,2)")]
        public decimal TargetValue { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; } = 0m;

         public DateTime? EstimatedDueDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

         [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedHours { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualHours { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CalculatedValue
        {
            get
            {
                if (string.Equals(Name, "OTD", StringComparison.OrdinalIgnoreCase) &&
                    EstimatedDueDate.HasValue &&
                    ActualEndDate.HasValue)
                {
                    return ActualEndDate.Value.Date <= EstimatedDueDate.Value.Date ? 100m : 0m;
                }

                if (string.Equals(Name, "Effectiveness", StringComparison.OrdinalIgnoreCase) &&
                    EstimatedHours > 0 &&
                    ActualHours > 0)
                {
                    return Math.Round((EstimatedHours / ActualHours) * 100, 2);
                }

                return CurrentValue > 0 ? CurrentValue : null;
            }
        }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

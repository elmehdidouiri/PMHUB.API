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
        public decimal? CalculatedValue =>
            Name switch
            {
                "OTD" when EstimatedDueDate.HasValue && ActualEndDate.HasValue =>
                    Math.Max(0, 100 - ((decimal)(ActualEndDate.Value - EstimatedDueDate.Value).TotalDays
                        / Math.Max(1, (decimal)(EstimatedDueDate.Value - DateTime.UtcNow).TotalDays)) * 100),

                "Effectiveness" when EstimatedHours > 0 && ActualHours > 0 =>
                    Math.Round((EstimatedHours / ActualHours) * 100, 2),

                _ => CurrentValue > 0 ? CurrentValue : null
            };

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
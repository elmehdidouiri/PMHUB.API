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

        // Distinguishes a deliberately entered 0% from a value that must be calculated.
        public bool IsManualValue { get; set; }

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
                if (string.Equals(Name, "OTD", StringComparison.OrdinalIgnoreCase))
                {
                    // A manually entered OTD always takes precedence over the date-based value.
                    if (IsManualValue || CurrentValue > 0)
                        return CurrentValue;

                    var dueDate = EstimatedDueDate ?? Project?.EstimatedDueDate;
                    var endDate = ActualEndDate ?? Project?.EndDate;
                    var startDate = Project?.StartDate;

                    if (!dueDate.HasValue || !endDate.HasValue)
                        return null;

                    if (endDate.Value.Date <= dueDate.Value.Date)
                        return 100m;

                    if (!startDate.HasValue || endDate.Value <= startDate.Value)
                        return null;

                    var plannedDuration = dueDate.Value - startDate.Value;
                    var actualDuration = endDate.Value - startDate.Value;
                    if (plannedDuration <= TimeSpan.Zero || actualDuration <= TimeSpan.Zero)
                        return null;

                    return Math.Round(Math.Min(100m, (decimal)(plannedDuration.TotalDays / actualDuration.TotalDays) * 100m), 2);
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

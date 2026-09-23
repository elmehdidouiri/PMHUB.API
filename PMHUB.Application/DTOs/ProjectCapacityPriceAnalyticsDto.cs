using PMHUB.Domain.Enums;

namespace PMHUB.Application.DTOs
{
    /// <summary>
    /// Portfolio capacity and cost indicators. The existing analytics filters
    /// (project, BU, department, plant, status and phase) can be used here.
    /// Targets are the project estimate and budget; actuals cover the complete
    /// project lifetime so they remain meaningful when the dashboard period is changed.
    /// </summary>
    public class ProjectCapacityPriceDashboardDto
    {
        public int ProjectCount { get; set; }
        public CapacityTargetDto CapacityTarget { get; set; } = new();
        public PriceTargetDto PriceTarget { get; set; } = new();
        public List<ProjectCapacityPriceDto> Projects { get; set; } = new();
    }

    public class ProjectCapacityPriceDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; }
        public ProjectPhase Phase { get; set; }
        public decimal BookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal RemainingHours { get; set; }
        public decimal VarianceHours { get; set; }
        public decimal CapacityAchievementPercentage { get; set; }
        public decimal LabourCost { get; set; }
        public decimal ResourceCost { get; set; }
        public decimal BookedPrice { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal RemainingPrice { get; set; }
        public decimal VariancePrice { get; set; }
        public decimal PriceAchievementPercentage { get; set; }
    }
}

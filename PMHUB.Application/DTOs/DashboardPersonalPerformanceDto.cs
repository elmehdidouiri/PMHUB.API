namespace PMHUB.Application.DTOs
{
    public class DashboardPersonalPerformanceDto
    {
        public DashboardPersonalPerformanceSummaryDto Summary { get; set; } = new();
        public DashboardPersonalPerformanceChartsDto Charts { get; set; } = new();
        public List<DashboardPersonalProjectContributionDto> TopProjects { get; set; } = new();
    }

    public class DashboardPersonalPerformanceSummaryDto
    {
        public decimal TotalLoggedHours { get; set; }
        public decimal YtdLoggedHours { get; set; }
        public decimal ExpectedHours { get; set; }
        public decimal UtilizationRate { get; set; }
        public decimal AverageHoursPerLoggedDay { get; set; }
        public int LoggedDays { get; set; }
        public int ProjectsWithLoggedHours { get; set; }
        public int AssignedProjects { get; set; }
        public int DelayedAssignedProjects { get; set; }
        public decimal PremiumApprovedHours { get; set; }
        public decimal PremiumPendingHours { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AnnualGoalProgressPercentage { get; set; }
    }

    public class DashboardPersonalPerformanceChartsDto
    {
        public List<DashboardLabelValueDto> HoursByCategory { get; set; } = new();
        public List<DashboardLabelValueDto> HoursByStage { get; set; } = new();
        public List<DashboardMonthlyHoursByCategoryDto> MonthlyHoursByCategory { get; set; } = new();
        public List<DashboardLabelValueDto> PremiumHours { get; set; } = new();
    }

    public class DashboardPersonalProjectContributionDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal TotalCost { get; set; }
        public decimal ProjectProgressPercentage { get; set; }
        public DateTime? EstimatedDueDate { get; set; }
        public bool IsDelayed { get; set; }
    }
}

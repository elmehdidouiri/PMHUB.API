namespace PMHUB.Application.DTOs
{
    public class DashboardOverviewDto
    {
        public DashboardSummaryDto Summary { get; set; } = new();
        public DashboardChartsDto Charts { get; set; } = new();
    }

    public class DashboardSummaryDto
    {
        public int TotalProjects { get; set; }
        public decimal TotalEstimatedHours { get; set; }
        public decimal TotalTrackedHours { get; set; }
        public decimal YtdHours { get; set; }
        public decimal AverageOtd { get; set; }
        public decimal AverageEffectiveness { get; set; }
        public int DelayedProjects { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int ApprovedUsers { get; set; }
    }

    public class DashboardChartsDto
    {
        public List<DashboardLabelValueDto> ProjectsByStatus { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByPhase { get; set; } = new();
        public List<TopProjectByHoursDto> TopProjectsByHours { get; set; } = new();
        public List<UsersByRoleDto> UsersByRole { get; set; } = new();
        public List<UsersByRoleDto> ProjectTeamMembersByRole { get; set; } = new();
        public List<DashboardLabelValueDto> MonthlyHoursBreakdownByCategory { get; set; } = new();
        public List<DashboardLabelValueDto> HoursByStage { get; set; } = new();
        public List<DashboardLabelValueDto> DeliveryMetrics { get; set; } = new();
    }

    public class DashboardLabelValueDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class TopProjectByHoursDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class UsersByRoleDto
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

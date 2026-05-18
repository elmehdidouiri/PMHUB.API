namespace PMHUB.Application.DTOs
{
    public class DashboardGroupedDistributionDto
    {
        public List<DashboardProjectGroupDto> ProjectManagement { get; set; } = new();
        public List<DashboardProjectGroupDto> ProjectTypes { get; set; } = new();
        public List<DashboardProjectGroupDto> Status { get; set; } = new();
        public List<DashboardProjectGroupDto> Phases { get; set; } = new();
        public List<DashboardProjectGroupDto> BusinessUnits { get; set; } = new();
        public List<DashboardProjectGroupDto> Departments { get; set; } = new();
        public List<DashboardProjectGroupDto> Plants { get; set; } = new();
    }

    public class DashboardProjectGroupDto
    {
        public string Label { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<ProjectSummaryDto> Projects { get; set; } = new();
    }

    public class DashboardGroupedDistributionCountsDto
    {
        public DashboardGroupedDistributionSummaryDto Summary { get; set; } = new();
        public List<DashboardProjectCountGroupDto> ProjectManagement { get; set; } = new();
        public List<DashboardProjectCountGroupDto> ProjectTypes { get; set; } = new();
        public List<DashboardProjectCountGroupDto> Status { get; set; } = new();
        public List<DashboardProjectCountGroupDto> Phases { get; set; } = new();
        public List<DashboardProjectCountGroupDto> BusinessUnits { get; set; } = new();
        public List<DashboardProjectCountGroupDto> Departments { get; set; } = new();
        public List<DashboardProjectCountGroupDto> Plants { get; set; } = new();
    }

    public class DashboardGroupedDistributionSummaryDto
    {
        public int TotalProjects { get; set; }
        public decimal AverageOtd { get; set; }
        public decimal AverageEffectiveness { get; set; }
        public int DelayedProjects { get; set; }
        public int DoneProjectsAboveTarget { get; set; }
        public int DoneProjectsBelowTarget { get; set; }
    }

    public class DashboardProjectCountGroupDto
    {
        public string Label { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

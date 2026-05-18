namespace PMHUB.Application.DTOs
{
    public class DashboardAdminBiDto
    {
        public DashboardBiFiltersDto Filters { get; set; } = new();
        public DashboardBiKpisDto Kpis { get; set; } = new();
        public DashboardBiChartsDto Charts { get; set; } = new();
        public DashboardBiTablesDto Tables { get; set; } = new();
        public List<DashboardAlertDto> Alerts { get; set; } = new();
    }

    public class DashboardBiFiltersDto
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool Ytd { get; set; }
        public string? ProjectStatus { get; set; }
        public string? ProjectPhase { get; set; }
        public string? ProcessStatus { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? BusinessUnitId { get; set; }
        public Guid? PlantId { get; set; }
        public Guid? ProjectManagerId { get; set; }
        public Guid? RoleId { get; set; }
        public string? ProjectType { get; set; }
        public string? ProjectManagementType { get; set; }
        public int TopN { get; set; }
    }

    public class DashboardBiKpisDto
    {
        public int TotalProjects { get; set; }
        public decimal TotalTrackedHours { get; set; }
        public int DelayedProjects { get; set; }
        public decimal DelayRate { get; set; }
        public decimal AverageOtd { get; set; }
        public decimal AverageEffectiveness { get; set; }
        public int DoneProjectsBelowTarget { get; set; }
        public int DoneProjectsAboveTarget { get; set; }
        public int ActiveUsers { get; set; }
        public decimal TrackedHoursVariancePercent { get; set; }
        public int ProjectsKpiDelta { get; set; }
        public decimal TrackedHoursDeltaPercent { get; set; }
        public int DelayedProjectsDelta { get; set; }
    }

    public class DashboardBiChartsDto
    {
        public List<DashboardLabelValueDto> ProjectsByStatus { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByPhase { get; set; } = new();
        public List<DashboardLabelValueDto> DelayRate { get; set; } = new();
        public List<DashboardLabelValueDto> HoursByCategory { get; set; } = new();
        public List<DashboardLabelValueDto> HoursByStage { get; set; } = new();
        public List<DashboardMonthlyHoursByCategoryDto> MonthlyHoursByCategory { get; set; } = new();
        public List<DashboardBiMonthlyTrendDto> MonthlyWorkloadTrend { get; set; } = new();
        public List<DashboardBiPerformanceTrendDto> PerformanceTrend { get; set; } = new();
        public List<DashboardLabelValueDto> WorkloadByRole { get; set; } = new();
        public List<DashboardLabelValueDto> WorkloadByDepartment { get; set; } = new();
        public List<DashboardLabelValueDto> WorkloadByBusinessUnit { get; set; } = new();
        public List<DashboardLabelValueDto> CostSavingByDepartment { get; set; } = new();
        public List<DashboardLabelValueDto> CostSavingByBusinessUnit { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByPlant { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByBusinessUnit { get; set; } = new();
        public List<DashboardEstimatedVsActualProjectDto> EstimatedVsActualProjects { get; set; } = new();
        public List<DashboardRiskMatrixProjectDto> RiskMatrix { get; set; } = new();
    }

    public class DashboardBiTablesDto
    {
        public List<DashboardTopProjectDto> TopProjectsByHours { get; set; } = new();
        public List<DashboardTopProjectDto> ProjectsOverEstimatedHours { get; set; } = new();
        public List<DashboardTopProjectDto> TopDelayedProjects { get; set; } = new();
        public List<DashboardTopProjectDto> TopOnHoldProjects { get; set; } = new();
        public List<DashboardAttentionProjectDto> DueSoonProjects { get; set; } = new();
        public List<DashboardAttentionProjectDto> NoRecentActivityProjects { get; set; } = new();
        public List<DashboardRoadblockDto> OpenRoadblocks { get; set; } = new();
    }

    public class DashboardBiMonthlyTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal Cost { get; set; }
    }

    public class DashboardBiPerformanceTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Otd { get; set; }
        public decimal Effectiveness { get; set; }
    }

    public class DashboardEstimatedVsActualProjectDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
        public decimal Budget { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DashboardRiskMatrixProjectDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal DelayDays { get; set; }
        public decimal RemainingProgress { get; set; }
        public decimal RiskScore { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DashboardAttentionProjectDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public DateTime? EstimatedDueDate { get; set; }
        public DateTime? LastLoggedAt { get; set; }
        public decimal TotalHours { get; set; }
        public decimal Value { get; set; }
    }

    public class DashboardRoadblockDto
    {
        public Guid RoadblockId { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EnteredAt { get; set; }
        public DateTime DueAt { get; set; }
        public decimal DelayDays { get; set; }
    }
}

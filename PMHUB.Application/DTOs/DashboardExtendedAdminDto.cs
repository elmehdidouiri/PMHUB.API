namespace PMHUB.Application.DTOs
{
    public class DashboardExtendedAdminDto
    {
        public DashboardAdminSummaryDto Summary { get; set; } = new();
        public DashboardExtendedAdminChartsDto Charts { get; set; } = new();
    }

    public class DashboardAdminSummaryDto
    {
        public int TotalProjects { get; set; }
        public decimal AverageOtd { get; set; }
        public decimal AverageEffectiveness { get; set; }
        public int DelayedProjects { get; set; }
        public int DoneProjectsAboveTarget { get; set; }
        public int DoneProjectsBelowTarget { get; set; }
    }

    public class DashboardExtendedAdminChartsDto
    {
        public List<DashboardLabelValueDto> DeliveryMetrics { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByBusinessUnit { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByDepartment { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByPlant { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByStatus { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByPhase { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByProjectManagementType { get; set; } = new();
    }

    public class DashboardPortfolioHealthDto
    {
        public int TotalProjects { get; set; }
        public int OngoingProjects { get; set; }
        public int PlannedProjects { get; set; }
        public int OnHoldProjects { get; set; }
        public int DoneProjects { get; set; }
        public int DelayedProjects { get; set; }
        public int DoneProjectsBelowTarget { get; set; }
        public int DoneProjectsAboveTarget { get; set; }
        public decimal AverageProgress { get; set; }
        public decimal AverageOtd { get; set; }
        public decimal AverageEffectiveness { get; set; }
        public List<DashboardLabelValueDto> ProjectsByStatus { get; set; } = new();
        public List<DashboardLabelValueDto> ProjectsByPhase { get; set; } = new();
    }

    public class DashboardWorkloadDto
    {
        public decimal TotalTrackedHours { get; set; }
        public decimal YtdHours { get; set; }
        public decimal PremiumApprovedHours { get; set; }
        public decimal PremiumPendingHours { get; set; }
        public List<DashboardMonthlyHoursByCategoryDto> MonthlyHoursByCategory { get; set; } = new();
        public List<DashboardLabelValueDto> CategoryBreakdown { get; set; } = new();
        public List<DashboardLabelValueDto> HoursByStage { get; set; } = new();
    }

    public class DashboardMonthlyHoursByCategoryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal ExecutionHours { get; set; }
        public decimal SupervisionHours { get; set; }
        public decimal ProcessHours { get; set; }
        public decimal ManagementHours { get; set; }
        public decimal RAndDHours { get; set; }
        public decimal WorkshopHours { get; set; }
        public decimal OtherHours { get; set; }
        public decimal InternManagementHours { get; set; }
    }

    public class DashboardUsersDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int ApprovedUsers { get; set; }
        public int PendingApprovalUsers { get; set; }
        public List<UsersByRoleDto> UsersByRole { get; set; } = new();
    }

    public class DashboardRisksDto
    {
        public int OpenRoadblocks { get; set; }
        public int OverdueRoadblocks { get; set; }
        public int DelayedProjects { get; set; }
        public int OnHoldProjects { get; set; }
        public int DueSoonProjects { get; set; }
    }

    public class DashboardBusinessDto
    {
        public decimal TotalBudget { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalEstimatedHours { get; set; }
        public decimal TotalTrackedHours { get; set; }
        public decimal TotalCostSaving { get; set; }
        public decimal TotalDigitalContribution { get; set; }
        public decimal BudgetConsumptionPercentage { get; set; }
    }

    public class DashboardTopProjectDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public decimal ProgressPercentage { get; set; }
        public decimal TotalHours { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal Budget { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime? EstimatedDueDate { get; set; }
        public bool IsDelayed { get; set; }
    }

    public class DashboardAlertDto
    {
        public string Severity { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}

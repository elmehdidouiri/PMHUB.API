namespace PMHUB.Application.DTOs
{
    public class AnalyticsQueryDto
    {
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? FiscalYear { get; set; }
        public string? PeriodMode { get; set; }
        public string? QuickSelect { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? BusinessUnitId { get; set; }
        public Guid? PlantId { get; set; }
        public string? ProjectStatus { get; set; }
        public string? ProjectPhase { get; set; }
    }

    public class AnalyticsDashboardDto
    {
        public AnalyticsPeriodDto Period { get; set; } = new();
        public AnalyticsSummaryDto Summary { get; set; } = new();
        public AnalyticsKpisDto Kpis { get; set; } = new();
        public AnalyticsHoursDto Hours { get; set; } = new();
    }

    public class AnalyticsPeriodDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int FiscalYear { get; set; }
        public int FiscalYearStartMonth { get; set; }
        public DateOnly FiscalYearStartDate { get; set; }
        public DateOnly FiscalYearEndDate { get; set; }
    }

    public class AnalyticsSummaryDto
    {
        public decimal AverageEffectiveness { get; set; }
        public decimal AverageOtd { get; set; }
        public decimal AverageCsat { get; set; }
        public int TotalProjects { get; set; }
        public int ProjectsWithData { get; set; }
        public decimal TotalHours { get; set; }
        public decimal YtdHours { get; set; }
        public decimal AverageMonthlyHours { get; set; }
        public decimal AverageUtilization { get; set; }
        public int ActiveTeamMembers { get; set; }
    }

    public class AnalyticsKpisDto
    {
        public List<AnalyticsKpiMonthlyTrendDto> MonthlyTrend { get; set; } = new();
    }

    public class AnalyticsKpiMonthlyTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Effectiveness { get; set; }
        public decimal Otd { get; set; }
        public decimal Csat { get; set; }
        public int ProjectsWithData { get; set; }
    }

    public class AnalyticsHoursDto
    {
        public List<AnalyticsMonthlyHoursByCategoryDto> MonthlyByCategory { get; set; } = new();
        public List<AnalyticsUtilizationTrendDto> UtilizationTrend { get; set; } = new();
        public List<AnalyticsBusinessUnitEffortDto> ByBusinessUnit { get; set; } = new();
    }

    public class AnalyticsMonthlyHoursByCategoryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal ExecutionHours { get; set; }
        public decimal TechnicalSupervisionHours { get; set; }
        public decimal ProcessHours { get; set; }
        public decimal ProjectManagementHours { get; set; }
        public decimal ResearchAndDevHours { get; set; }
        public decimal WorkshopHours { get; set; }
        public decimal OtherHours { get; set; }
        public decimal InternManagementHours { get; set; }
        public decimal TotalHours { get; set; }
    }

    public class AnalyticsUtilizationTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal LoggedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }

    public class AnalyticsBusinessUnitEffortDto
    {
        public Guid BusinessUnitId { get; set; }
        public string BusinessUnitName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int ProjectCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class AnalyticsFiltersDto
    {
        public List<int> FiscalYears { get; set; } = new();
        public List<AnalyticsMonthOptionDto> Months { get; set; } = new();
        public List<AnalyticsOptionDto<Guid>> Users { get; set; } = new();
        public List<AnalyticsOptionDto<Guid>> Projects { get; set; } = new();
        public List<AnalyticsOptionDto<Guid>> Departments { get; set; } = new();
        public List<AnalyticsOptionDto<Guid>> BusinessUnits { get; set; } = new();
        public List<AnalyticsOptionDto<Guid>> Plants { get; set; } = new();
    }

    public class AnalyticsOptionDto<T>
    {
        public T Id { get; set; } = default!;
        public string Label { get; set; } = string.Empty;
    }

    public class AnalyticsMonthOptionDto
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}

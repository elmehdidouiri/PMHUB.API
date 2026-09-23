using System;
using System.Collections.Generic;

namespace PMHUB.Application.DTOs
{
    public class BookingTargetComparisonQueryDto
    {
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? FiscalYear { get; set; }
        public string? PeriodMode { get; set; }
        public string? QuickSelect { get; set; }
        public decimal HourlyRate { get; set; } = 27m;
        public decimal? TargetHoursPerMember { get; set; }
        public string CalculationMode { get; set; } = "Brut"; // "Brut" or "Net"
        public Guid? ProjectId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? BusinessUnitId { get; set; }
        public Guid? PlantId { get; set; }
    }

    public class BookingTargetComparisonDashboardDto
    {
        public AnalyticsPeriodDto Period { get; set; } = new();
        public int FiscalYear { get; set; }
        public string PeriodMode { get; set; } = "ytd";
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public decimal TargetHoursPerMember { get; set; }
        public string CalculationMode { get; set; } = "Brut";
        public BookingTargetSummaryDto Summary { get; set; } = new();
        public List<PopulationComparisonDto> Populations { get; set; } = new();
        public List<BookingTargetMonthlyTrendDto> MonthlyTrend { get; set; } = new();
    }

    public class BookingTargetSummaryDto
    {
        public int TotalPeople { get; set; }
        public decimal BookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal RemainingHours { get; set; }
        public decimal VarianceHours { get; set; }
        public decimal HoursAchievementPercentage { get; set; }
        public decimal BookedRevenue { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal RemainingRevenue { get; set; }
        public decimal VarianceRevenue { get; set; }
        public decimal RevenueAchievementPercentage { get; set; }

        public decimal GrossBookedHours { get; set; }
        public decimal NetBookedHours { get; set; }
        public decimal GrossBookedRevenue { get; set; }
        public decimal NetBookedRevenue { get; set; }

        // Population breakdown for frontend direct binding
        public PopulationComparisonDto Employees { get; set; } = new();
        public PopulationComparisonDto Subcontractors { get; set; } = new();
        public PopulationComparisonDto Interns { get; set; } = new();
        public PopulationComparisonDto Total { get; set; } = new();
    }

    public class PopulationComparisonDto
    {
        public string Label { get; set; } = string.Empty;
        public string Population { get; set; } = string.Empty; // "Employees", "Subcontractors", "Interns", "Total"
        public string PopulationKey { get; set; } = string.Empty; // "employees", "subcontractors", "interns", "total"
        public int Headcount { get; set; }
        public int People { get; set; }
        public decimal BookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal RemainingHours { get; set; }
        public decimal VarianceHours { get; set; }
        public decimal HoursAchievementPercentage { get; set; }
        public decimal HoursAchievementPercent { get; set; }
        public decimal BookedRevenue { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal RemainingRevenue { get; set; }
        public decimal VarianceRevenue { get; set; }
        public decimal RevenueAchievementPercentage { get; set; }
        public decimal RevenueAchievementPercent { get; set; }

        // Specific to Employees (for Subcontractors and Interns, Gross == Net)
        public decimal GrossBookedHours { get; set; }
        public decimal NetBookedHours { get; set; }
        public decimal GrossHoursAchievementPercentage { get; set; }
        public decimal NetHoursAchievementPercentage { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal GrossBookedRevenue { get; set; }
        public decimal NetBookedRevenue { get; set; }
        public decimal GrossRevenueAchievementPercentage { get; set; }
        public decimal NetRevenueAchievementPercentage { get; set; }
    }

    public class BookingTargetMonthlyTrendDto
    {
        public int FiscalMonth { get; set; }
        public int FiscalMonthIndex { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;

        // Monthly total metrics
        public decimal BookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal HoursAchievementPercentage { get; set; }
        public decimal BookedRevenue { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal RevenueAchievementPercentage { get; set; }

        public decimal GrossBookedHours { get; set; }
        public decimal NetBookedHours { get; set; }

        // Cumulative (YTD trajectory) metrics
        public decimal CumulativeBookedHours { get; set; }
        public decimal CumulativeTargetHours { get; set; }
        public decimal CumulativeHoursAchievementPercentage { get; set; }
        public decimal CumulativeBookedRevenue { get; set; }
        public decimal CumulativeTargetRevenue { get; set; }
        public decimal CumulativeRevenueAchievementPercentage { get; set; }

        public decimal CumulativeGrossBookedHours { get; set; }
        public decimal CumulativeNetBookedHours { get; set; }

        // Population breakdown for this month
        public PopulationMonthlyMetricDto Employees { get; set; } = new();
        public PopulationMonthlyMetricDto Subcontractors { get; set; } = new();
        public PopulationMonthlyMetricDto Interns { get; set; } = new();
    }

    public class PopulationMonthlyMetricDto
    {
        public int People { get; set; }
        public decimal BookedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal HoursAchievementPercentage { get; set; }
        public decimal BookedRevenue { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal RevenueAchievementPercentage { get; set; }

        public decimal GrossBookedHours { get; set; }
        public decimal NetBookedHours { get; set; }
        public decimal GrossBookedRevenue { get; set; }
        public decimal NetBookedRevenue { get; set; }
    }
}

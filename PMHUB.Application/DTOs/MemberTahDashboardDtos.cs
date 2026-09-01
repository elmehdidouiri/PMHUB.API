using PMHUB.Domain.Enums;

namespace PMHUB.Application.DTOs
{
    public class MemberTahQueryDto : AnalyticsQueryDto
    {
        /// <summary>
        /// Optional override of the admin TAH monthly hours target (default 171.9).
        /// </summary>
        public decimal? TahMonthlyHoursTarget { get; set; }
    }

    public class MemberTahDashboardDto
    {
        public AnalyticsPeriodDto Period { get; set; } = new();
        public decimal TahMonthlyHoursTarget { get; set; }
        public MemberTahSummaryDto Summary { get; set; } = new();
        public List<MemberTahMonthlyBreakdownDto> MonthlyBreakdown { get; set; } = new();
        public List<MemberTahMemberDto> Members { get; set; } = new();
    }

    public class MemberTahSummaryDto
    {
        public int EmployeeCount { get; set; }
        public int SubcontractorCount { get; set; }
        public decimal AverageEffectiveness { get; set; }
        public decimal CumulativeTahHours { get; set; }
        public decimal EmployeeTahHours { get; set; }
        public decimal SubcontractorTahHours { get; set; }
        public decimal EmployeeSharePercentage { get; set; }
        public decimal SubcontractorSharePercentage { get; set; }
    }

    public class MemberTahMonthlyBreakdownDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public int SubcontractorCount { get; set; }
        public decimal AverageEffectiveness { get; set; }
        public decimal TahHours { get; set; }
        public decimal EmployeeTahHours { get; set; }
        public decimal SubcontractorTahHours { get; set; }
        public decimal EmployeeSharePercentage { get; set; }
        public decimal SubcontractorSharePercentage { get; set; }
    }

    public class MemberTahMemberDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public MemberType MemberType { get; set; }
        public string MemberTypeLabel { get; set; } = string.Empty;
        public decimal BookedHours { get; set; }
        public decimal Effectiveness { get; set; }
        public decimal TahHours { get; set; }
        public List<MemberTahMemberMonthDto> Monthly { get; set; } = new();
    }

    public class MemberTahMemberMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal BookedHours { get; set; }
        public decimal Effectiveness { get; set; }
        public decimal TahHours { get; set; }
    }
}

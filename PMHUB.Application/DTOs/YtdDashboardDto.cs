using System.Collections.Generic;

namespace PMHUB.Application.DTOs
{
    public class YtdDashboardDto
    {
        public int CompanyYear { get; set; }
        public string FiscalYearLabel => $"{CompanyYear - 1}/{CompanyYear}";

        public decimal YtdHours { get; set; }
        public decimal ExpectedHours { get; set; }
        public decimal Variance { get; set; }
        public decimal ProjectedYearEnd { get; set; }
        public decimal MonthlyRecommendation { get; set; }

        public decimal YtdCost { get; set; }
        public decimal PremiumHours { get; set; }
        public decimal PremiumApprovedCost { get; set; }
        public decimal PremiumPendingHours { get; set; }

        public List<MonthlyHoursDashboardDto> MonthlyBreakdown { get; set; } = new();

        public decimal CompletionPercentage => ExpectedHours > 0 ? (YtdHours / ExpectedHours) * 100 : 0;
        public string PerformanceStatus => CompletionPercentage >= 100 ? "Excellent" : CompletionPercentage >= 90 ? "Bon" : CompletionPercentage >= 75 ? "Moyen" : "Faible";
    }
}

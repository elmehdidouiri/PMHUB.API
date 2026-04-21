using System;
using System.Collections.Generic;

namespace PMHUB.Application.DTOs
{
    public class MonthlyHoursDashboardDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");

        public decimal LoggedHours { get; set; }
        public decimal TargetHours { get; set; }
        public decimal Variance { get; set; }
        public decimal TotalCost { get; set; }

        public int WorkingDays { get; set; }
        public decimal DailyTarget { get; set; }
        public int DaysLeft { get; set; }
        public decimal DailyNeeded { get; set; }
        public decimal Progress { get; set; }

        public decimal PremiumHours { get; set; }
        public decimal PremiumPendingHours { get; set; }

        public decimal TotalExecutionHours { get; set; }
        public decimal TotalSupervisionHours { get; set; }
        public decimal TotalProcessHours { get; set; }
        public decimal TotalManagementHours { get; set; }
        public decimal TotalRAndDHours { get; set; }
        public decimal TotalWorkshopHours { get; set; }
        public decimal TotalOtherHours { get; set; }
        public decimal TotalInternManagementHours { get; set; }

        public List<HourEntrySummaryDto> Entries { get; set; } = new();

        public bool IsOnTrack => Variance >= 0;
        public string Status => Progress >= 100 ? "Objectif atteint" : Progress >= 80 ? "En bonne voie" : "En retard";
    }
}
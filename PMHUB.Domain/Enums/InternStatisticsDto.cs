using System;
using System.Collections.Generic;

namespace PMHUB.Application.DTOs
{
     /// Statistiques globales pour un stagiaire
     public class InternStatisticsDto
    {
        public Guid InternId { get; set; }
        public string InternName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public string SupervisorEmail { get; set; } = string.Empty;
        
         public decimal TotalAllocatedHours { get; set; }
        public decimal TotalWorkedHours { get; set; }
        public decimal TotalRemainingHours { get; set; }
        public decimal UtilizationRate { get; set; }  
        
         public int TotalProjectAllocations { get; set; }
        public List<InternProjectAllocationStatsDto> ProjectAllocations { get; set; } = new();
        
         public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

     /// Statistiques par projet pour un stagiaire
     public class InternProjectAllocationStatsDto
    {
        public Guid AllocationId { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectStatus { get; set; } = string.Empty;
        public string ProjectPhase { get; set; } = string.Empty;
        
        public decimal AllocatedHours { get; set; }
        public decimal WorkedHours { get; set; }
        public decimal RemainingHours { get; set; }
        public decimal HoursPercentage { get; set; }  
        
        public int TotalHourEntries { get; set; }
        public decimal AverageHoursPerEntry { get; set; }
        
        public DateTime AllocationDate { get; set; }
        public DateTime? LastLoggedDate { get; set; }
        public string? Notes { get; set; }
    }

     /// Visualisation temporelle des heures travaillées par un stagiaire
     public class InternWorkVisualizationDto
    {
        public Guid InternId { get; set; }
        public string InternName { get; set; } = string.Empty;
        
         public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDaysInRange { get; set; }
        public int DaysWithWork { get; set; }
        
         public decimal TotalHours { get; set; }
        public decimal AverageHoursPerDay { get; set; }
        public decimal AverageHoursPerWorkDay { get; set; }
        
         public List<InternWorkProjectBreakdownDto> ProjectBreakdowns { get; set; } = new();
        
         public List<InternDailyWorkDto> DailyWork { get; set; } = new();
    }

    public class InternWorkProjectBreakdownDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal Hours { get; set; }
        public decimal Percentage { get; set; }
        public int EntryCount { get; set; }
    }

    public class InternDailyWorkDto
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int EntryCount { get; set; }
        public List<InternDailyProjectBreakdownDto> ProjectBreakdown { get; set; } = new();
    }

    public class InternDailyProjectBreakdownDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal Hours { get; set; }
    }

     /// Résumé mensuel/annuel pour un stagiaire
     public class InternPeriodStatisticsDto
    {
        public Guid InternId { get; set; }
        public string InternName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int? Month { get; set; } // null si annuel
        public string PeriodLabel { get; set; } = string.Empty;
        
        public decimal TotalHours { get; set; }
        public decimal AllocatedHoursInPeriod { get; set; }
        public decimal UtilizationRate { get; set; }
        
        public int WorkDays { get; set; }
        public decimal AverageHoursPerDay { get; set; }
        
        public List<InternPeriodProjectStatsDto> ProjectBreakdowns { get; set; } = new();
    }

    public class InternPeriodProjectStatsDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal Hours { get; set; }
        public decimal Percentage { get; set; }
    }

     /// Dashboard agrégé pour tous les stagiaires
     public class InternsDashboardDto
    {
        public int TotalInterns { get; set; }
        public int ActiveInterns { get; set; }
        public int InactiveInterns { get; set; }
        
        public decimal TotalAllocatedHours { get; set; }
        public decimal TotalWorkedHours { get; set; }
        public decimal OverallUtilizationRate { get; set; }
        
        public int TotalProjectAllocations { get; set; }
        public int TotalHourEntries { get; set; }
        
        public List<InternSummaryStatsDto> InternsSummary { get; set; } = new();
        public List<ProjectInternAllocationSummaryDto> TopProjectAllocations { get; set; } = new();
    }

    public class InternSummaryStatsDto
    {
        public Guid InternId { get; set; }
        public string InternName { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        
        public decimal TotalAllocatedHours { get; set; }
        public decimal TotalWorkedHours { get; set; }
        public decimal UtilizationRate { get; set; }
        
        public int ProjectCount { get; set; }
        public int HourEntryCount { get; set; }
    }

    public class ProjectInternAllocationSummaryDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        
        public int InternCount { get; set; }
        public decimal TotalAllocatedHours { get; set; }
        public decimal TotalWorkedHours { get; set; }
        public decimal UtilizationRate { get; set; }
    }
}
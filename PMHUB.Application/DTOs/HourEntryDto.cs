using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;

namespace PMHUB.Application.DTOs
{
    public class HourEntryDto
    {
        public Guid Id { get; set; }

         public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;

         public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

         public AllocationType AllocationType { get; set; }
        public ProjectType ProjectType { get; set; }

         public DateTime Date { get; set; }

         public decimal ExecutionHours { get; set; }
        public decimal SupervisionHours { get; set; }
        public decimal ProcessHours { get; set; }
        public decimal ManagementHours { get; set; }
        public decimal RAndDHours { get; set; }
        public decimal WorkshopHours { get; set; }
        public decimal OtherHours { get; set; }
        public decimal InternManagementHours { get; set; }

         public decimal TotalHours { get; set; }

         public decimal HourlyRate { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; } = "DH";

         public bool IsPremium { get; set; }
        public string? PremiumReason { get; set; }
        public string? PremiumApprovalStatus { get; set; }

         public string? Notes { get; set; }
        public List<DetailInternSupervisionDto> SupervisedInterns { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class DetailInternSupervisionDto
    {
        public Guid InternId { get; set; }
        public string InternName { get; set; } = string.Empty;
        public decimal Hours { get; set; }
    }
}
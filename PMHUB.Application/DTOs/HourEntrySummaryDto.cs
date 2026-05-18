 using PMHUB.Domain.Enums;
using System;

namespace PMHUB.Application.DTOs
{
    public class HourEntrySummaryDto
    {
        public Guid Id { get; set; }
        public CategoryWork Category { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        public decimal TotalHours { get; set; }

         public decimal TotalCost { get; set; }
        public string Currency { get; set; } = "EUR";

        public bool IsPremium { get; set; }
        public string PremiumStatus { get; set; } = string.Empty;

        public AllocationType AllocationType { get; set; }
        public ProjectType ProjectType { get; set; }
    }
}
